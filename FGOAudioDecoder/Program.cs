using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FGOAudioDecoder;

internal class Program
{
    private static string[] arguments;

    private static async Task DisplayMenu()
    {
        Console.Clear();
        try
        {
            int arg;
            /*Console.WriteLine(
                "------FGOAudioDecoder------\n" +
                "1: cpk2wav(单文件)\t" +
                "2: cpk2wav(选择文件夹批量转换)\n" +
                "3: Usm2(m2v+wav)(单文件)\t" +
                "4: Usm2(m2v+wav)(选择文件夹批量转换)\n" +
                "5: Usm2(m2v+wav)(国服自打包用的不同格式，仅供选项3、4发生失败时单独使用)\n" +
                "6: DebugMode\n" +
                "999: 退出程序\n" +
                "请选择功能..."
            );*/

            Console.WriteLine(
                "------FGOAudioDecoder------\n" +
                "1: cpk2wav(单文件)\t" +
                "2: cpk2wav(选择文件夹批量转换)\n" +
                "3: Usm2(m2v+wav)(单文件)\t" +
                "4: Usm2(m2v+wav)(选择文件夹批量转换)\n" +
                "5: Usm2(m2v+wav)(国服自打包用的不同格式，仅供选项3、4发生失败时单独使用)\n" +
                "999: 退出程序\n" +
                "请选择功能..."
            );

            if (arguments == null)
            {
                try
                {
                    arg = Convert.ToInt32(Console.ReadLine());
                }
                catch (Exception)
                {
                    arg = -1;
                }
            }
            else
            {
                arg = Convert.ToInt32(arguments[1]);
                Console.WriteLine(arg);
            }

            switch (arg)
            {
                case 1:
                    while (arguments == null)
                    {
                        Console.WriteLine("请将cpk文件拖入窗口内获取路径,并按回车键:");
                        var filepath = Console.ReadLine().Replace("\"", "");
                        var file = new FileInfo(filepath);
                        var outputfolder = file.DirectoryName;
                        var output = new DirectoryInfo(outputfolder);
                        DecryptCpkFileNew(file, output);
                        Thread.Sleep(1000);
                        Console.WriteLine("解包完成,点击任意键继续...");
                        Console.ReadKey(true);
                        await DisplayMenu();
                    }

                    break;
                case 2:
                    while (arguments == null)
                    {
                        Console.WriteLine("请将放有cpk文件的文件夹目录拖入窗口内获取路径,并按回车键:");
                        var cpkfolderpath = Console.ReadLine().Replace("\"", "");
                        var cpkfolder = new DirectoryInfo(cpkfolderpath);
                        foreach (var cpkfile in cpkfolder.GetFiles("*.cpk.bytes", SearchOption.AllDirectories))
                            await Task.Run(async () => { await DecryptCpkFileNew(cpkfile, cpkfolder); });
                        Thread.Sleep(1000);
                        Console.WriteLine("解包完成,点击任意键继续...");
                        Console.ReadKey(true);
                        await DisplayMenu();
                        break;
                    }

                    Console.WriteLine("请将放有cpk文件的文件夹目录拖入窗口内获取路径,并按回车键:");
                    var cpkfolderpathquick = arguments[0];
                    Console.WriteLine(cpkfolderpathquick);
                    var cpkfolderquick = new DirectoryInfo(cpkfolderpathquick);
                    foreach (var cpkfile2 in cpkfolderquick.GetFiles("*.cpk.bytes", SearchOption.AllDirectories))
                        await Task.Run(async () => { await DecryptCpkFileNew(cpkfile2, cpkfolderquick); });
                    Thread.Sleep(1000);
                    Console.WriteLine("解包完成,点击任意键继续...");
                    break;
                case 3:
                    while (arguments == null)
                    {
                        Console.WriteLine("请将Usm文件拖入窗口内获取路径,并按回车键:");
                        var filepathusm = Console.ReadLine().Replace("\"", "");
                        var fileusm = new FileInfo(filepathusm);
                        FGOAudioDecoder.DecodeUsmFiles(fileusm);
                        Thread.Sleep(1000);
                        Console.WriteLine("解包完成,点击任意键继续...");
                        Console.ReadKey(true);
                        await DisplayMenu();
                    }

                    break;
                case 4:
                    while (arguments == null)
                    {
                        Console.WriteLine("请将放有Usm文件的文件夹目录拖入窗口内获取路径,并按回车键:");
                        var usmfolderpath = Console.ReadLine().Replace("\"", "");
                        var usmfolder = new DirectoryInfo(usmfolderpath);
                        foreach (var usmfile in usmfolder.GetFiles("*.usm", SearchOption.AllDirectories))
                            FGOAudioDecoder.DecodeUsmFiles(usmfile);
                        Thread.Sleep(1000);
                        Console.WriteLine("解包完成,点击任意键继续...");
                        Console.ReadKey(true);
                        await DisplayMenu();
                        break;
                    }

                    Console.WriteLine("请将放有Usm文件的文件夹目录拖入窗口内获取路径,并按回车键:");
                    var usmfolderpathQuick = arguments[0];
                    Console.WriteLine(usmfolderpathQuick);
                    var usmfolderQuick = new DirectoryInfo(usmfolderpathQuick);
                    foreach (var usmfile2 in usmfolderQuick.GetFiles("*.usm", SearchOption.AllDirectories))
                        FGOAudioDecoder.DecodeUsmFiles(usmfile2);
                    Thread.Sleep(1000);
                    Console.WriteLine("解包完成,点击任意键继续...");
                    break;
                case 5:
                    while (arguments == null)
                    {
                        Console.WriteLine("请将Usm文件拖入窗口内获取路径,并按回车键:");
                        var filepathusm = Console.ReadLine().Replace("\"", "");
                        var fileusm = new FileInfo(filepathusm);
                        FGOAudioDecoder.DecodeBGOSpecialUsmFiles(fileusm);
                        Thread.Sleep(1000);
                        Console.WriteLine("解包完成,点击任意键继续...");
                        Console.ReadKey(true);
                        await DisplayMenu();
                    }

                    break;
                /*case 6:
                    while (arguments == null)
                    {
                        Console.WriteLine("请将cpk文件拖入窗口内获取路径,并按回车键:");
                        var filepath = Console.ReadLine().Replace("\"", "");
                        var file = new FileInfo(filepath);
                        var outputfolder = file.DirectoryName;
                        var output = new DirectoryInfo(outputfolder);
                        DecryptCpkFileDebug(file, output);
                        Thread.Sleep(1000);
                        Console.WriteLine("解包完成,点击任意键继续...");
                        Console.ReadKey(true);
                        await DisplayMenu();
                    }
                    break;*/
                case 999:
                    return;
                default:
                    Console.WriteLine("请输入一个有效选项,点击任意键重新选择...");
                    Console.ReadKey(true);
                    await DisplayMenu();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine("遇到错误,点击任意键重新开始...");
            Console.ReadKey(true);
            await DisplayMenu();
        }
    }

    private static async Task DecryptCpkFile(FileInfo file, DirectoryInfo outputfolder)
    {
        Console.WriteLine("解包音频: " + file.Name);
        var acbFilename = "";
        await Task.Run(() =>
        {
            try
            {
                acbFilename = FGOAudioDecoder.UnpackCpkFiles(file, outputfolder);
            }
            catch (Exception)
            {
                //ignore
            }
        });
        if (acbFilename == "")
        {
            Console.WriteLine("该文件不是cpk类型文件.请重试.");
            return;
        }

        await Task.Run(() => { FGOAudioDecoder.DecodeAcbFiles(new FileInfo(acbFilename), outputfolder); });
        Thread.Sleep(1500);
    }

    private static async Task DecryptCpkFileNew(FileInfo file, DirectoryInfo outputfolder)
    {
        Console.WriteLine("解包音频: " + file.Name);
        var acbFilename = "";
        await Task.Run(() =>
        {
            try
            {
                acbFilename = FGOAudioDecoder.UnpackCpkFiles(file, outputfolder);
            }
            catch (Exception)
            {
                //ignore
            }
        });
        if (acbFilename == "")
        {
            Console.WriteLine("该文件不是cpk类型文件.请重试.");
            return;
        }

        await Task.Run(() => { FGOAudioDecoder.DecodeAcbFilesNew(new FileInfo(acbFilename), outputfolder); });
        Thread.Sleep(1500);
    }

    private static async Task Main(string[] args)
    {
        var path = Directory.GetCurrentDirectory();
        if (!File.Exists(path + @"\crid.exe"))
            ExtractExe.ExtractResFile("FGOAudioDecoder.Application.crid.exe", path + @"\crid.exe");
        if (!File.Exists(path + @"\ffmpeg.exe"))
            ExtractExe.ExtractResFile("FGOAudioDecoder.Application.ffmpeg.exe", path + @"\ffmpeg.exe");
        if (args.Length != 0)
            arguments = args;
        await DisplayMenu();
        while (arguments == null)
        {
            Console.WriteLine("点按任意键退出...");
            Console.ReadKey(true);
        }
    }
}