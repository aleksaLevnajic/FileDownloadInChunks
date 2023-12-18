using System.Diagnostics;
using System.Net;

namespace FileDownload
{
    internal class Program
    {

        static async Task Main()
        {
            string fileUrl = "https://sample-videos.com/img/Sample-jpg-image-20mb.jpg";
            string destinationPath = "C:\\Users\\Alexa\\Desktop\\Akvelon Internship\\File_To_Save";
            int numberOfChunks = 5; // more chunks, faster the download
            var stopwatch = new Stopwatch();
            
            stopwatch.Start();
            await DownloadFileInChunks(fileUrl, destinationPath, numberOfChunks);
            stopwatch.Stop();
            Console.WriteLine(stopwatch.Elapsed.ToString());
            
        }

        static async Task DownloadFileInChunks(string fileUrl, string destinationPath, int numberOfChunks)
        {
            using (var httpClient = new HttpClient())
            {
                var headRequest = new HttpRequestMessage(HttpMethod.Head, fileUrl);
                var headResponse = await httpClient.SendAsync(headRequest);

                var fileSize = long.Parse(headResponse.Content.Headers.GetValues("Content-Length").First());
                var chunkSize = fileSize / numberOfChunks;

                var tasks = new Task[numberOfChunks];
                var progress = 0;

                for (int i = 0; i < numberOfChunks; i++)
                {
                    var startByte = i * chunkSize;
                    long endByte;

                    if(i == numberOfChunks - 1)
                    {
                        endByte = fileSize - 1;
                    }    
                    else
                    {
                        endByte = startByte + (chunkSize - 1);
                    }
                    

                    tasks[i] = DownloadChunkAsync(fileUrl, destinationPath + $".part{i}", startByte, endByte);

                    tasks[i].ContinueWith(task =>  //without await(11 sec), with await(41.9)
                    {
                        Console.WriteLine($"Chunk {i} downloaded.");
                        progress += 100 / numberOfChunks;
                        DisplayProgressBar(progress);
                    }, TaskContinuationOptions.OnlyOnRanToCompletion);
                }

                await Task.WhenAll(tasks);

                Console.WriteLine("Merging chunk files...");
                MergeChunkFiles(destinationPath, numberOfChunks);

                Console.WriteLine("Deleting temporary files...");
                DeleteChunkFiles(destinationPath, numberOfChunks);

                Console.WriteLine("Download completed.");
            }
        }

        static async Task DownloadChunkAsync(string fileUrl, string partFilePath, long startByte, long endByte)
        {
            using (var httpClient = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, fileUrl);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(startByte, endByte);

                var response = await httpClient.SendAsync(request);
                var content = await response.Content.ReadAsByteArrayAsync();

                await File.WriteAllBytesAsync(partFilePath, content);
            }
        }

        static void MergeChunkFiles(string destinationPath, int numberOfChunks)
        {
            using (var finalFileStream = new FileStream(destinationPath, FileMode.Create))
            {
                for (int i = 0; i < numberOfChunks; i++)
                {
                    var chunkFilePath = $"{destinationPath}.part{i}";
                    var chunkBytes = File.ReadAllBytes(chunkFilePath);
                    finalFileStream.Write(chunkBytes, 0, chunkBytes.Length);
                }
            }
        }

        static void DeleteChunkFiles(string destinationPath, int numberOfChunks)
        {
            for (int i = 0; i < numberOfChunks; i++)
            {
                var chunkFilePath = $"{destinationPath}.part{i}";
                File.Delete(chunkFilePath);
            }
        }

        static void DisplayProgressBar(int progress)
        {
            Console.Write($"\rDownloading... {progress}% complete");
        }

        
    }
}
