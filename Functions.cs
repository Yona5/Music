using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage.Blob;
using NAudio.Wave;


namespace Music_WebJob
{
    public class Functions
    {
        // This class contains the application-specific WebJob code consisting of event-driven
        // methods executed when messages appear in queues with any supporting code.

        // Trigger method  - run when new message detected in queue. "samplemaker" is name of queue.
        // "musicgallery" is name of storage container; "songs" and "samples" are folder names. 
        // "{queueTrigger}" is an inbuilt variable taking on value of contents of message automatically;
        // the other variables are valued automatically.
        public static void GenerateSamples(
                [QueueTrigger("samplemaker")] String blobInfo,
                [Blob("musicgallery/songs/{queueTrigger}")] CloudBlockBlob inputBlob,
                [Blob("musicgallery/samples/{queueTrigger}")] CloudBlockBlob outputBlob, TextWriter logger
            )
        {
            //use log.WriteLine() rather than Console.WriteLine() for trace output
            logger.WriteLine("GenerateSamples() started...");
            logger.WriteLine("Input blob is: " + blobInfo);

            // Open streams to blobs for reading and writing as appropriate.
            // Pass references to application specific methods
            try
            {
                using (Stream input = inputBlob.OpenRead())
                using (Stream output = outputBlob.OpenWrite())
                {
                    CreateSample(input, output, 20);
                    outputBlob.Properties.ContentType = "audio/mpeg3";
                }
                logger.WriteLine("GenerateSamples() completed...");
                outputBlob.Metadata["Title"] = inputBlob.Metadata["Title"];
                outputBlob.SetMetadata();
            }
            catch (Exception) { }
            
        }

        // Create sample - the detail is unimportant but notice formal parameter types.
        private static void CreateSample(Stream input, Stream output, int duration)
        {
            using (var reader = new Mp3FileReader(input, wave => new NLayer.NAudioSupport.Mp3FrameDecompressor(wave)))
            {
                Mp3Frame frame;
                frame = reader.ReadNextFrame();
                int frameTimeLength = (int)(frame.SampleCount / (double)frame.SampleRate * 1000.0);
                int framesRequired = (int)(duration / (double)frameTimeLength * 1000.0);

                int frameNumber = 0;
                while ((frame = reader.ReadNextFrame()) != null)
                {
                    frameNumber++;

                    if (frameNumber <= framesRequired)
                    {
                        output.Write(frame.RawData, 0, frame.RawData.Length);
                    }
                    else break;
                }
            }
        }
    }
}
