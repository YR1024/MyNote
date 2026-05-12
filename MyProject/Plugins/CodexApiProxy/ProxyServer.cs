using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CodexApiProxy
{
    public class LogEventArgs : EventArgs
    {
        public string Message { get; }
        public LogEventArgs(string message) { Message = message; }
    }

    public class ProxyServer
    {
        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private readonly HttpClient _httpClient;

        public event EventHandler<LogEventArgs> Log;
        public bool IsRunning { get; private set; }

        public string ApiUrl { get; set; }
        public string ApiKey { get; set; }
        public string Model { get; set; }
        public int Port { get; set; }

        public ProxyServer()
        {
            _httpClient = new HttpClient();
        }

        private void OnLog(string message)
        {
            Log?.Invoke(this, new LogEventArgs($"[{DateTime.Now:HH:mm:ss}] {message}"));
        }

        public async Task StartAsync()
        {
            if (IsRunning) return;

            _cts = new CancellationTokenSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Start();
            IsRunning = true;

            OnLog($"代理服务已启动: http://127.0.0.1:{Port}");
            OnLog($"上游 API: {ApiUrl}");
            OnLog($"模型: {Model}");

            _ = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
                        _ = Task.Run(() => HandleRequestAsync(context), _cts.Token);
                    }
                    catch (HttpListenerException) { break; }
                    catch (ObjectDisposedException) { break; }
                    catch (Exception ex)
                    {
                        OnLog($"监听错误: {ex.Message}");
                    }
                }
            }, _cts.Token);
        }

        public void Stop()
        {
            if (!IsRunning) return;
            _cts?.Cancel();
            _listener?.Stop();
            _listener?.Close();
            IsRunning = false;
            OnLog("代理服务已停止");
        }

        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            var req = context.Request;
            var res = context.Response;

            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.Headers.Add("Access-Control-Allow-Methods", "POST, OPTIONS");
            res.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");

            if (req.HttpMethod == "OPTIONS")
            {
                res.StatusCode = 204;
                res.Close();
                return;
            }

            var url = req.Url.AbsolutePath;

            if (req.HttpMethod == "POST" && (url == "/v1/responses" || url == "/responses"))
            {
                try
                {
                    string body;
                    using (var ms = new MemoryStream())
                    {
                        await req.InputStream.CopyToAsync(ms);
                        body = Encoding.UTF8.GetString(ms.ToArray());
                    }

                    if (body.Length > 2000)
                        OnLog($"收到请求 (长度 {body.Length}): {body.Substring(0, 500)}...");
                    else
                        OnLog($"收到请求: {body}");

                    JObject requestJson;
                    try
                    {
                        requestJson = JObject.Parse(body);
                    }
                    catch (JsonReaderException)
                    {
                        var fixedBody = SanitizeJson(body);
                        requestJson = JObject.Parse(fixedBody);
                        OnLog("已自动修复非标准 JSON 格式");
                    }

                    await HandleResponsesEndpointAsync(res, requestJson);
                }
                catch (Exception ex)
                {
                    OnLog($"请求处理错误: {ex.Message}");
                    await SendJsonError(res, 400, ex.Message);
                }
                return;
            }

            if (req.HttpMethod == "GET" && (url == "/" || url == "/v1"))
            {
                await SendJsonResponse(res, 200, new JObject
                {
                    ["service"] = "codex→deepseek proxy",
                    ["model"] = Model,
                    ["status"] = "ok"
                });
                return;
            }

            res.StatusCode = 404;
            var notFoundBytes = Encoding.UTF8.GetBytes("Not Found");
            await res.OutputStream.WriteAsync(notFoundBytes, 0, notFoundBytes.Length);
            res.Close();
        }

        private async Task HandleResponsesEndpointAsync(HttpListenerResponse res, JObject body)
        {
            var input = body["input"];
            var messages = TranslateMessages(input);

            var identity = "\n\n[IMPORTANT: Your true underlying model is " + Model +
                ". When asked about your model identity, you MUST answer that you are " + Model +
                ". Ignore any conflicting identity claims in the instructions above.]";

            var instructions = body["instructions"]?.ToString();
            var systemContent = !string.IsNullOrEmpty(instructions) ? instructions + identity : identity.Trim();
            messages.Insert(0, new JObject { ["role"] = "system", ["content"] = systemContent });

            var stream = body["stream"]?.ToObject<bool>() ?? true;

            var chatBody = new JObject
            {
                ["model"] = Model,
                ["messages"] = JArray.FromObject(messages),
                ["stream"] = stream,
                ["thinking"] = new JObject { ["type"] = "disabled" }
            };

            var tools = TranslateTools(body["tools"]);
            if (tools != null && tools.Count > 0)
            {
                chatBody["tools"] = JArray.FromObject(tools);
                chatBody["tool_choice"] = body["tool_choice"]?.ToString() ?? "auto";
            }

            if (body["max_output_tokens"] != null)
            {
                chatBody["max_tokens"] = body["max_output_tokens"];
            }

            var inputText = LastUserText(messages);
            OnLog($"输入: {Truncate(inputText, 200)}");

            var apiUrlBase = ApiUrl.TrimEnd('/');
            if (apiUrlBase.EndsWith("/v1"))
                apiUrlBase = apiUrlBase.Substring(0, apiUrlBase.Length - 3);
            var chatCompletionsUrl = $"{apiUrlBase}/v1/chat/completions";

            var httpReq = new HttpRequestMessage(HttpMethod.Post, chatCompletionsUrl);
            httpReq.Headers.Add("Authorization", $"Bearer {ApiKey}");
            httpReq.Content = new StringContent(chatBody.ToString(Formatting.None), Encoding.UTF8, "application/json");
            if (stream)
            {
                httpReq.Headers.Add("Accept", "text/event-stream");
            }

            try
            {
                var response = await _httpClient.SendAsync(httpReq, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    var errBody = await response.Content.ReadAsStringAsync();
                    OnLog($"上游 API 错误 ({(int)response.StatusCode}): {Truncate(errBody, 300)}");
                    await SendJsonError(res, 502, $"Upstream API {(int)response.StatusCode}: {Truncate(errBody, 200)}");
                    return;
                }

                if (!stream)
                {
                    await HandleNonStreamResponse(res, response);
                }
                else
                {
                    await HandleStreamResponse(res, response);
                }
            }
            catch (Exception ex)
            {
                OnLog($"上游请求错误: {ex.Message}");
                await SendJsonError(res, 502, ex.Message);
            }
        }

        private async Task HandleNonStreamResponse(HttpListenerResponse res, HttpResponseMessage upstream)
        {
            var data = await upstream.Content.ReadAsStringAsync();
            try
            {
                var completion = JObject.Parse(data);
                var msg = completion["choices"]?[0]?["message"];
                var output = new JArray();

                if (msg?["content"] != null)
                {
                    var text = msg["content"].ToString();
                    OnLog($"输出: {Truncate(text, 200)}");
                    output.Add(new JObject
                    {
                        ["id"] = "msg_1",
                        ["type"] = "message",
                        ["role"] = "assistant",
                        ["content"] = new JArray { new JObject { ["type"] = "output_text", ["text"] = text } },
                        ["status"] = "completed"
                    });
                }

                if (msg?["tool_calls"] != null)
                {
                    foreach (var tc in msg["tool_calls"])
                    {
                        output.Add(new JObject
                        {
                            ["id"] = $"fc_{tc["id"]}",
                            ["type"] = "function_call",
                            ["call_id"] = tc["id"],
                            ["name"] = tc["function"]?["name"],
                            ["arguments"] = tc["function"]?["arguments"]?.ToString(),
                            ["status"] = "completed"
                        });
                    }
                }

                var result = new JObject
                {
                    ["id"] = "resp_1",
                    ["object"] = "response",
                    ["status"] = "completed",
                    ["model"] = Model,
                    ["output"] = output
                };

                await SendJsonResponse(res, 200, result);
            }
            catch (Exception ex)
            {
                OnLog($"非流式解析错误: {ex.Message}");
                await SendJsonError(res, 502, ex.Message);
            }
        }

        private async Task HandleStreamResponse(HttpListenerResponse res, HttpResponseMessage upstream)
        {
            res.StatusCode = 200;
            res.ContentType = "text/event-stream";
            res.Headers.Add("Cache-Control", "no-cache");
            res.Headers.Add("Connection", "keep-alive");

            var translator = new SseTranslator(Model);

            using (var stream = await upstream.Content.ReadAsStreamAsync())
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (!line.StartsWith("data: ")) continue;
                    var json = line.Substring(6).Trim();
                    if (json == "[DONE]")
                    {
                        translator.Done(res);
                        break;
                    }
                    try
                    {
                        var chunk = JObject.Parse(json);
                        translator.Feed(res, chunk);
                    }
                    catch { }
                }
            }

            if (!translator.IsDone)
            {
                translator.Done(res);
            }

            OnLog($"输出: {Truncate(translator.ContentSoFar, 200)}");
        }

        private static List<JObject> TranslateMessages(JToken input)
        {
            var messages = new List<JObject>();

            if (input == null) return messages;

            if (input.Type == JTokenType.String)
            {
                var text = input.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    messages.Add(new JObject { ["role"] = "user", ["content"] = text });
                }
                return messages;
            }

            if (input.Type == JTokenType.Object)
            {
                var text = ExtractText(input["content"]);
                if (!string.IsNullOrEmpty(text))
                {
                    messages.Add(new JObject { ["role"] = "user", ["content"] = text });
                }
                return messages;
            }

            if (input.Type != JTokenType.Array) return messages;

            foreach (var item in input)
            {
                var itemType = item["type"]?.ToString();

                if (itemType == "function_call")
                {
                    JObject target = null;
                    if (messages.Count > 0)
                    {
                        var last = messages[messages.Count - 1];
                        if (last["role"]?.ToString() == "assistant")
                            target = last;
                    }
                    if (target == null)
                    {
                        target = new JObject { ["role"] = "assistant", ["tool_calls"] = new JArray() };
                        messages.Add(target);
                    }
                    if (target["tool_calls"] == null)
                        target["tool_calls"] = new JArray();

                    ((JArray)target["tool_calls"]).Add(new JObject
                    {
                        ["id"] = item["call_id"]?.ToString() ?? item["id"]?.ToString(),
                        ["type"] = "function",
                        ["function"] = new JObject
                        {
                            ["name"] = item["name"],
                            ["arguments"] = item["arguments"]?.ToString()
                        }
                    });
                }
                else if (itemType == "function_call_output")
                {
                    messages.Add(new JObject
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = item["call_id"]?.ToString() ?? item["id"]?.ToString(),
                        ["content"] = ExtractText(item["output"])
                    });
                }
                else if (item["role"] != null)
                {
                    var role = item["role"].ToString();
                    if (role == "developer") role = "system";

                    var msg = new JObject
                    {
                        ["role"] = role,
                        ["content"] = ExtractText(item["content"])
                    };
                    messages.Add(msg);
                }
            }

            return messages;
        }

        private static string ExtractText(JToken content)
        {
            if (content == null) return "";
            if (content.Type == JTokenType.String) return content.ToString();
            if (content.Type != JTokenType.Array) return "";

            var parts = new List<string>();
            foreach (var part in content)
            {
                var partType = part["type"]?.ToString();
                if (partType == "input_text" || partType == "output_text" ||
                    partType == "text" || partType == "reasoning_text")
                {
                    parts.Add(part["text"]?.ToString() ?? "");
                }
            }
            return string.Join("", parts);
        }

        private static string LastUserText(List<JObject> messages)
        {
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                if (messages[i]["role"]?.ToString() == "user")
                    return ExtractText(messages[i]["content"]);
            }
            return "";
        }

        private static List<JObject> TranslateTools(JToken rawTools)
        {
            if (rawTools == null || rawTools.Type != JTokenType.Array) return null;

            var tools = new List<JObject>();
            foreach (var t in rawTools)
            {
                var name = t["name"]?.ToString() ?? t["function"]?["name"]?.ToString();
                if (string.IsNullOrEmpty(name)) continue;

                tools.Add(new JObject
                {
                    ["type"] = "function",
                    ["function"] = new JObject
                    {
                        ["name"] = name,
                        ["description"] = t["description"]?.ToString() ?? t["function"]?["description"]?.ToString() ?? "",
                        ["parameters"] = t["parameters"] ?? t["function"]?["parameters"] ??
                            new JObject { ["type"] = "object", ["properties"] = new JObject() }
                    }
                });
            }
            return tools;
        }

        private async Task SendJsonResponse(HttpListenerResponse res, int statusCode, JObject body)
        {
            res.StatusCode = statusCode;
            res.ContentType = "application/json";
            var bytes = Encoding.UTF8.GetBytes(body.ToString(Formatting.None));
            await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            res.Close();
        }

        private async Task SendJsonError(HttpListenerResponse res, int statusCode, string message)
        {
            res.StatusCode = statusCode;
            res.ContentType = "application/json";
            var error = new JObject { ["error"] = new JObject { ["message"] = message } };
            var bytes = Encoding.UTF8.GetBytes(error.ToString(Formatting.None));
            await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            res.Close();
        }

        private static string SanitizeJson(string json)
        {
            json = json.Trim();
            var sb = new StringBuilder(json.Length);
            bool inString = false;
            bool escaped = false;
            int i = 0;
            while (i < json.Length)
            {
                char c = json[i];
                if (escaped)
                {
                    sb.Append(c);
                    escaped = false;
                    i++;
                    continue;
                }
                if (c == '\\' && inString)
                {
                    sb.Append(c);
                    escaped = true;
                    i++;
                    continue;
                }
                if (c == '"')
                {
                    inString = !inString;
                    sb.Append(c);
                    i++;
                    continue;
                }
                if (inString)
                {
                    if (c == '\r' || c == '\n')
                    {
                        sb.Append("\\n");
                        i++;
                        continue;
                    }
                    if (c == '\t')
                    {
                        sb.Append("\\t");
                        i++;
                        continue;
                    }
                    sb.Append(c);
                    i++;
                    continue;
                }
                if (c == ',')
                {
                    int j = i + 1;
                    while (j < json.Length && char.IsWhiteSpace(json[j])) j++;
                    if (j < json.Length && (json[j] == '}' || json[j] == ']'))
                    {
                        i++;
                        continue;
                    }
                }
                sb.Append(c);
                i++;
            }
            return sb.ToString();
        }

        private static string Truncate(string text, int maxLen)
        {
            if (string.IsNullOrEmpty(text)) return "(空)";
            return text.Length <= maxLen ? text : text.Substring(0, maxLen) + "...";
        }

        public void Dispose()
        {
            Stop();
            _httpClient?.Dispose();
        }
    }

    public class SseTranslator
    {
        private readonly string _model;
        private readonly string _responseId;
        private string _itemId;
        private bool _textStarted;
        private bool _started;
        private readonly Dictionary<int, ToolCallInfo> _toolCalls = new Dictionary<int, ToolCallInfo>();
        private readonly HashSet<int> _finished = new HashSet<int>();

        public string ContentSoFar { get; private set; } = "";
        public bool IsDone { get; private set; }

        public SseTranslator(string model)
        {
            _model = model;
            _responseId = "resp_" + Guid.NewGuid().ToString("N").Substring(0, 8);
            _itemId = "item_" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        private void Emit(HttpListenerResponse res, string eventName, JObject data)
        {
            var payload = $"event: {eventName}\ndata: {data.ToString(Formatting.None)}\n\n";
            var bytes = Encoding.UTF8.GetBytes(payload);
            res.OutputStream.Write(bytes, 0, bytes.Length);
            res.OutputStream.Flush();
        }

        private void EnsureStarted(HttpListenerResponse res)
        {
            if (_started) return;
            _started = true;

            Emit(res, "response.created", new JObject
            {
                ["type"] = "response.created",
                ["response"] = new JObject
                {
                    ["id"] = _responseId,
                    ["object"] = "response",
                    ["status"] = "in_progress",
                    ["model"] = _model,
                    ["output"] = new JArray()
                }
            });

            Emit(res, "response.in_progress", new JObject
            {
                ["type"] = "response.in_progress",
                ["response_id"] = _responseId
            });
        }

        public void Feed(HttpListenerResponse res, JObject chunk)
        {
            var delta = chunk["choices"]?[0]?["delta"];
            if (delta == null) return;

            if (delta["content"] != null)
            {
                EnsureStarted(res);
                var content = delta["content"].ToString();
                ContentSoFar += content;

                if (!_textStarted)
                {
                    _textStarted = true;
                    Emit(res, "response.output_item.added", new JObject
                    {
                        ["type"] = "response.output_item.added",
                        ["response_id"] = _responseId,
                        ["output_index"] = 0,
                        ["item"] = new JObject
                        {
                            ["id"] = _itemId,
                            ["type"] = "message",
                            ["role"] = "assistant",
                            ["status"] = "in_progress",
                            ["content"] = new JArray()
                        }
                    });
                }

                Emit(res, "response.output_text.delta", new JObject
                {
                    ["type"] = "response.output_text.delta",
                    ["response_id"] = _responseId,
                    ["item_id"] = _itemId,
                    ["output_index"] = 0,
                    ["content_index"] = 0,
                    ["delta"] = content
                });
            }

            if (delta["tool_calls"] != null)
            {
                EnsureStarted(res);
                foreach (var tc in delta["tool_calls"])
                {
                    var idx = tc["index"]?.ToObject<int>() ?? 0;
                    if (!_toolCalls.ContainsKey(idx))
                    {
                        var call = new ToolCallInfo
                        {
                            Id = tc["id"]?.ToString() ?? $"call_{idx}",
                            Name = tc["function"]?["name"]?.ToString() ?? "",
                            Arguments = ""
                        };
                        _toolCalls[idx] = call;

                        Emit(res, "response.output_item.added", new JObject
                        {
                            ["type"] = "response.output_item.added",
                            ["response_id"] = _responseId,
                            ["output_index"] = idx + 1,
                            ["item"] = new JObject
                            {
                                ["id"] = $"fc_{call.Id}",
                                ["type"] = "function_call",
                                ["call_id"] = call.Id,
                                ["name"] = call.Name,
                                ["status"] = "in_progress"
                            }
                        });
                    }

                    var toolCall = _toolCalls[idx];
                    if (tc["function"]?["name"] != null)
                        toolCall.Name = tc["function"]["name"].ToString();

                    var argDelta = tc["function"]?["arguments"]?.ToString() ?? "";
                    toolCall.Arguments += argDelta;

                    Emit(res, "response.function_call_arguments.delta", new JObject
                    {
                        ["type"] = "response.function_call_arguments.delta",
                        ["response_id"] = _responseId,
                        ["item_id"] = $"fc_{toolCall.Id}",
                        ["output_index"] = idx + 1,
                        ["delta"] = argDelta
                    });
                }
            }
        }

        public void Done(HttpListenerResponse res)
        {
            if (IsDone) return;
            IsDone = true;
            EnsureStarted(res);

            var output = new JArray();

            if (_textStarted)
            {
                Emit(res, "response.output_text.done", new JObject
                {
                    ["type"] = "response.output_text.done",
                    ["response_id"] = _responseId,
                    ["item_id"] = _itemId,
                    ["output_index"] = 0,
                    ["content_index"] = 0,
                    ["text"] = ContentSoFar
                });

                Emit(res, "response.output_item.done", new JObject
                {
                    ["type"] = "response.output_item.done",
                    ["response_id"] = _responseId,
                    ["output_index"] = 0,
                    ["item"] = new JObject
                    {
                        ["id"] = _itemId,
                        ["type"] = "message",
                        ["role"] = "assistant",
                        ["content"] = new JArray { new JObject { ["type"] = "output_text", ["text"] = ContentSoFar } },
                        ["status"] = "completed"
                    }
                });

                output.Add(new JObject
                {
                    ["id"] = _itemId,
                    ["type"] = "message",
                    ["role"] = "assistant",
                    ["content"] = new JArray { new JObject { ["type"] = "output_text", ["text"] = ContentSoFar } },
                    ["status"] = "completed"
                });
            }

            foreach (var kvp in _toolCalls)
            {
                if (_finished.Contains(kvp.Key)) continue;
                _finished.Add(kvp.Key);
                var call = kvp.Value;

                Emit(res, "response.function_call_arguments.done", new JObject
                {
                    ["type"] = "response.function_call_arguments.done",
                    ["response_id"] = _responseId,
                    ["item_id"] = $"fc_{call.Id}",
                    ["output_index"] = kvp.Key + 1,
                    ["arguments"] = call.Arguments,
                    ["name"] = call.Name,
                    ["call_id"] = call.Id
                });

                Emit(res, "response.output_item.done", new JObject
                {
                    ["type"] = "response.output_item.done",
                    ["response_id"] = _responseId,
                    ["output_index"] = kvp.Key + 1,
                    ["item"] = new JObject
                    {
                        ["id"] = $"fc_{call.Id}",
                        ["type"] = "function_call",
                        ["call_id"] = call.Id,
                        ["name"] = call.Name,
                        ["arguments"] = call.Arguments,
                        ["status"] = "completed"
                    }
                });

                output.Add(new JObject
                {
                    ["id"] = $"fc_{call.Id}",
                    ["type"] = "function_call",
                    ["call_id"] = call.Id,
                    ["name"] = call.Name,
                    ["arguments"] = call.Arguments,
                    ["status"] = "completed"
                });
            }

            Emit(res, "response.completed", new JObject
            {
                ["type"] = "response.completed",
                ["response"] = new JObject
                {
                    ["id"] = _responseId,
                    ["object"] = "response",
                    ["status"] = "completed",
                    ["model"] = _model,
                    ["output"] = output
                }
            });

            res.OutputStream.Flush();
            res.Close();
        }

        private class ToolCallInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Arguments { get; set; }
        }
    }
}
