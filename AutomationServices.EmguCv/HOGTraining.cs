using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;

namespace AutomationServices.EmguCv
{
    internal class HOGTraining
    {

        public void a()
        {

          

            // Load positive and negative image samples
            var positiveSamples = LoadSamples("positive_samples_folder");
            var negativeSamples = LoadSamples("negative_samples_folder");

            // Create HOG descriptor object
            var hog = new HOGDescriptor();

            // Compute HOG descriptors for positive and negative samples
            var positiveDescriptors = new List<float[]>();
            var negativeDescriptors = new List<float[]>();

            foreach (var sample in positiveSamples)
            {
                var descriptor = hog.Compute(sample)/*.ToFloatArray()*/;
                positiveDescriptors.Add(descriptor);
            }

            foreach (var sample in negativeSamples)
            {
                var descriptor = hog.Compute(sample)/*.ToFloatArray()*/;
                negativeDescriptors.Add(descriptor);
            }

            // Create training data and labels
            var trainingData = new Matrix<float>(positiveDescriptors.Count + negativeDescriptors.Count, positiveDescriptors[0].Length);
            var labels = new Matrix<int>(positiveDescriptors.Count + negativeDescriptors.Count, 1);

            for (int i = 0; i < positiveDescriptors.Count; i++)
            {
                for (int j = 0; j < positiveDescriptors[i].Length; j++)
                {
                    trainingData[i, j] = positiveDescriptors[i][j];
                }

                labels[i, 0] = 1; // Positive class label
            }

            for (int i = 0; i < negativeDescriptors.Count; i++)
            {
                for (int j = 0; j < negativeDescriptors[i].Length; j++)
                {
                    trainingData[positiveDescriptors.Count + i, j] = negativeDescriptors[i][j];
                }

                labels[positiveDescriptors.Count + i, 0] = -1; // Negative class label
            }


        }





        public static List<Image<Bgr, byte>> LoadSamples(string folderPath)
        {
            var samples = new List<Image<Bgr, byte>>();

            foreach (var imagePath in Directory.GetFiles(folderPath, "*.jpg"))
            {
                var image = new Image<Bgr, byte>(imagePath);
                var annotationPath = Path.ChangeExtension(imagePath, "txt");

                if (File.Exists(annotationPath))
                {
                    var annotation = File.ReadAllText(annotationPath).Split(' ');
                    var x1 = int.Parse(annotation[0]);
                    var y1 = int.Parse(annotation[1]);
                    var x2 = int.Parse(annotation[2]);
                    var y2 = int.Parse(annotation[3]);

                    var sample = image.Copy(new Rectangle(x1, y1, x2 - x1, y2 - y1));
                    samples.Add(sample);
                }
                else
                {
                    samples.Add(image);
                }
            }

            return samples;
        }

    }
}
