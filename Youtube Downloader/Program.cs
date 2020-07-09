using System;
using System.IO;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using System.Diagnostics;

namespace Youtube_Downloader
{
    class Program
    {
        static string VideoAddress { get; set; }
        static string VideoName { get; set; }
        static string ValidVideoName { get; set; }
        static string VideoPath { get; set; }
        static string StartTime { get; set; }
        static string EndTime { get; set; }
        static bool DoNeedToCut { get; set; } = false;
        static string DownloadFolderPath { get; } = Environment.CurrentDirectory + @"\download\";
        static bool DoNeedToRun { get; set; } = true;
        static string Extension { get; set; } = ".mp4";

        static string info = "ytd.exe [URL] => Download Youtube Video\n" +
                             "ytd.exe [URL] [T1] [T2] => Download Youtube Video and Cut it between T1{mm:ss} and T2{mm:ss} Time";

        static async Task Main(string[] args)
        {
            CreateDownloadFolder();

            SetProperties(args);

            if (DoNeedToRun) await DownloadAsync();
        }

        static void SetProperties(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(info);
                Console.WriteLine("Press Any Key to Exit");
                Console.ReadKey();
                DoNeedToRun = false;
                return;
            }

            VideoAddress = args[0];

            if (args.Length == 3)
            {
                StartTime = args[1];
                EndTime = args[2];

                DoNeedToCut = true;
            }
        }

        static async Task DownloadAsync()
        {
            VideoName = (await new YoutubeClient().Videos.GetAsync(VideoAddress)).Title;
            ValidVideoName = RemoveIllegalCharacters(VideoName);
            VideoPath = DownloadFolderPath + ValidVideoName + Extension;
            
            Console.WriteLine("\nDownloading ...");

            var youtube = new YoutubeClient();

            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(VideoAddress);

            var streamInfo = streamManifest.GetMuxed().WithHighestVideoQuality();

            if (streamInfo != null)
            {
                if (DoNeedToCut)
                {
                    var stream = await youtube.Videos.Streams.GetAsync(streamInfo);

                    var fileStream = new FileStream("temp" + Extension, FileMode.Create, FileAccess.Write);
                    stream.CopyTo(fileStream);
                    fileStream.Dispose();

                    if (File.Exists("temp" + Extension)) Console.WriteLine("\nDownloaded Successfully!");
                    else throw new FileLoadException("Video not downloaded correctly. Please check URL again.");

                    TrimVideo();

                    return;
                }

                await youtube.Videos.Streams.DownloadAsync(streamInfo, VideoPath);

                if (File.Exists(VideoPath)) Console.WriteLine("\nDownloaded Successfully!");
                else throw new FileLoadException("Video not downloaded correctly. Please check URL again.");
            }
        }

        static void CreateDownloadFolder()
        {
            if (!Directory.Exists(DownloadFolderPath)) Directory.CreateDirectory(DownloadFolderPath);
        }

        static void TrimVideo()
        {
            string args = $"-i temp{Extension} -ss 00:{StartTime} -to 00:{EndTime} -async 1 \"{VideoPath}\"";
            //Console.WriteLine(args);

            Process process = Process.Start("ffmpeg.exe", args);
            process.WaitForExit();
            File.Delete(Environment.CurrentDirectory + "\\temp" + Extension);

            if (File.Exists(VideoPath))Console.WriteLine("\nCutted Successfully!\n");
            else throw new FileLoadException("Video not cutted correctly. Please check arguments again.");
        }

        static string RemoveIllegalCharacters(string str)
        {
            var illegalChars = new char[] { '*', '<', '>', ':', '"', '/', '\\', '|', '?' };

            foreach (char c in illegalChars)
            {
                str = str.Replace(c, ' ');
            }

            return str;
        }
    }
}
