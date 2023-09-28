using App1;
using Android.Content;
using Android.Database;
using Android.Net;
using Android.Provider;
using Android.Telephony;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

[assembly: Dependency(typeof(SmsService))]

namespace App1
{
    public class SmsService : ISmsService
    {
        public async Task<string> ReadLatestSmsAsync()
        {
            try
            {
                string smsContent = string.Empty;

                string[] projection = { "body" };
                string sortOrder = "date desc";
                using (ICursor cursor = Android.App.Application.Context.ContentResolver.Query(Telephony.Sms.Inbox.ContentUri, projection, null, null, sortOrder))
                {
                    if (cursor.MoveToFirst())
                    {
                        smsContent = cursor.GetString(cursor.GetColumnIndexOrThrow("body"));
                    }
                }

                return smsContent;
            }
            catch (Exception ex)
            {
                // 处理异常
                Console.WriteLine("Error reading SMS: " + ex);
                return string.Empty;
            }
        }

        public async Task<string> ReadLatestSmsAsync(string phoneNumber)
        {
            try
            {
                string smsContent = string.Empty;

                string[] projection = { "body", "address",};
                string sortOrder = "date DESC";
                string selection = $"address = ?";
                string[] selectionArgs = { phoneNumber };
                using (ICursor cursor = Android.App.Application.Context.ContentResolver.Query(Telephony.Sms.Inbox.ContentUri, projection, selection, selectionArgs, sortOrder))
                {
                    if (cursor.MoveToFirst())
                    {
                        smsContent = cursor.GetString(cursor.GetColumnIndexOrThrow("body"));
                    }
                }

                return smsContent;
            }
            catch (Exception ex)
            {
                // 处理异常
                Console.WriteLine("Error reading SMS: " + ex);
                return string.Empty;
            }
        }

        public async Task<string> ReadLatestSmsAsync(DateTime time, string phoneNumber)
        {
            try
            {
                string smsContent = string.Empty;

                string[] projection = { "body", "address", "date" };
                string sortOrder = "date DESC";
                string selection = $"address = ? AND date > ?";
                //string[] selectionArgs = { phoneNumber, (DateTime.UtcNow.Subtract(time)).TotalMilliseconds.ToString() };
                string[] selectionArgs = { phoneNumber, (time.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds.ToString() };

                using (ICursor cursor = Android.App.Application.Context.ContentResolver.Query(Telephony.Sms.Inbox.ContentUri, projection, selection, selectionArgs, sortOrder))
                {
                    if (cursor.MoveToFirst())
                    {
                        smsContent = cursor.GetString(cursor.GetColumnIndexOrThrow("body"));
                    }
                }

                return smsContent;
            }
            catch (Exception ex)
            {
                // 处理异常
                Console.WriteLine("Error reading SMS: " + ex);
                return string.Empty;
            }
        }
    }

}
