using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DereTore.Exchange.Audio.HCA;
using VGMToolbox.format;
using static System.String;

namespace FGOAudioDecoder;

public static class FGOAudioDecoder
{
    public static string UnpackCpkFiles(FileInfo filename, DirectoryInfo AudioFolder)
    {
        var cpk_name = filename.FullName;
        var cpk = new CPK(new Tools());
        cpk.ReadCPK(cpk_name);
        var oldFile = new BinaryReader(File.OpenRead(cpk_name));
        List<FileEntry> entries = null;
        entries = cpk.FileTable.Where(x => x.FileType == "FILE").ToList();
        if (entries.Count == 0) return "";
        var filefullname = "";
        foreach (var t in entries)
        {
            if (!IsNullOrEmpty((string)t.DirName)) Directory.CreateDirectory(t.DirName.ToString());

            oldFile.BaseStream.Seek((long)t.FileOffset, SeekOrigin.Begin);
            var isComp = Encoding.ASCII.GetString(oldFile.ReadBytes(8));
            oldFile.BaseStream.Seek((long)t.FileOffset, SeekOrigin.Begin);

            var chunk = oldFile.ReadBytes(int.Parse(t.FileSize.ToString()));
            if (isComp == "CRILAYLA")
            {
                var size = int.Parse((t.ExtractSize ?? t.FileSize).ToString());
                chunk = cpk.DecompressCRILAYLA(chunk, size);
            }

            File.WriteAllBytes(AudioFolder.FullName + @"\" + t.FileName, chunk);
            if (t.FileName.ToString().Contains(".acb"))
                filefullname = AudioFolder.FullName + @"\" + t.FileName;
        }

        oldFile.Close();
        return filefullname;
    }

    public static void DecodeAcbFiles(FileInfo filename, DirectoryInfo AudioFolder) //已弃用，全面转换vgmstream
    {
        var volume = 1F;
        var mode = 16;
        var loop = 0;
        var ciphKey1 = 0x92EBF464;
        uint ciphKey2 = 0x7E896318;
        var dir = AudioFolder;
        var dir2 = new DirectoryInfo(AudioFolder.FullName + @"\DecodedWavs\");
        var acbfile = filename;
        var fs = new FileStream(acbfile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var af = new CriAcbFile(fs, 0, false);
        Console.WriteLine(filename.Name + " - 拆分acb文件...");
        af.ExtractAll();
        fs.Close();
        var destinationFolder = new DirectoryInfo(Path.Combine(acbfile.DirectoryName,
            "_vgmt_acb_ext_" + Path.GetFileNameWithoutExtension(acbfile.FullName)));
        var OutFolder =
            Path.Combine(Path.GetDirectoryName(acbfile.FullName.Replace(dir.FullName, dir2.FullName)),
                Path.GetFileNameWithoutExtension(acbfile.FullName));
        Directory.CreateDirectory(OutFolder);

        Parallel.ForEach(destinationFolder.GetFiles("*.hca", SearchOption.AllDirectories), hcafile =>
        {
            Console.WriteLine(hcafile.Name + " - 解密hca...");
            try
            {
                using var inputFileStream = File.Open(hcafile.FullName, FileMode.Open, FileAccess.Read);
                using var outputFileStream =
                    File.Open(OutFolder + @"\" + hcafile.Name.Substring(0, hcafile.Name.Length - 4) + ".wav",
                        FileMode.Create, FileAccess.Write);
                var decodeParams = DecodeParams.CreateDefault();
                decodeParams.Key1 = ciphKey1;
                decodeParams.Key2 = ciphKey2;
                decodeParams.KeyModifier = 0;

                var audioParams = AudioParams.CreateDefault();

                audioParams.InfiniteLoop = AudioParams.Default.InfiniteLoop;
                audioParams.SimulatedLoopCount = AudioParams.Default.SimulatedLoopCount;
                audioParams.OutputWaveHeader = true;

                using var hcaStream = new HcaAudioStream(inputFileStream, decodeParams, audioParams);
                var read = 1;
                var dataBuffer = new byte[1024];

                while (read > 0)
                {
                    read = hcaStream.Read(dataBuffer, 0, dataBuffer.Length);

                    if (read > 0) outputFileStream.Write(dataBuffer, 0, read);
                }
            }
            catch (Exception)
            {
                File.Delete(OutFolder + @"\" + hcafile.Name.Substring(0, hcafile.Name.Length - 4) + ".wav");
                Console.WriteLine(hcafile.Name + " - 解密时遇到错误.该文件为Criware v1.30+的打包版本，正在唤起vgmstream...");
                var path = Directory.GetCurrentDirectory();
                if (!File.Exists(path + @"\vgmstream-cli.exe"))
                {
                    Console.WriteLine(hcafile.Name +
                                      " - 唤起失败!请前往\"https://vgmstream.org/\"下载Command-line (64-bit) Win版本后解压至软件目录.");
                    return;
                }

                var vgmStreamCommands =
                    $"-o {OutFolder + @"\" + Path.GetFileNameWithoutExtension(hcafile.FullName)}.wav {hcafile.FullName}";
                var process = new Process
                {
                    StartInfo =
                    {
                        FileName = path + @"\vgmstream-cli.exe",
                        Arguments = vgmStreamCommands,
                        UseShellExecute = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit();
                Console.WriteLine(hcafile.Name + " - 使用vgmstream工具解包完成.");
            }

            File.Delete(hcafile.FullName);
        });
        var awbfilename = acbfile.FullName.Substring(0, acbfile.FullName.Length - 4) + ".awb";
        File.Delete(acbfile.FullName);
        File.Delete(awbfilename);
        Directory.Delete(destinationFolder.FullName, true);
    }

    public static void DecodeAcbFilesNew(FileInfo filename, DirectoryInfo AudioFolder)
    {
        var dir = AudioFolder;
        var dir2 = new DirectoryInfo(AudioFolder.FullName + @"\DecodedWavs\");
        var acbfile = filename;
        var fs = new FileStream(acbfile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var af = new CriAcbFile(fs, 0, false);
        Console.WriteLine(filename.Name + " - 拆分acb文件...");
        af.ExtractAll();
        fs.Close();
        var awbfilename = acbfile.FullName.Substring(0, acbfile.FullName.Length - 4) + ".awb";
        var destinationFolder = new DirectoryInfo(Path.Combine(acbfile.DirectoryName,
            "_vgmt_acb_ext_" + Path.GetFileNameWithoutExtension(acbfile.FullName)));
        var OutFolder =
            Path.Combine(Path.GetDirectoryName(acbfile.FullName.Replace(dir.FullName, dir2.FullName)),
                Path.GetFileNameWithoutExtension(acbfile.FullName));
        Directory.CreateDirectory(OutFolder);
        Console.WriteLine($"{Path.GetFileNameWithoutExtension(acbfile.FullName)} - 正在唤起vgmstream...");
        var path = Directory.GetCurrentDirectory();
        if (!File.Exists(path + @"\vgmstream-cli.exe"))
        {
            Console.WriteLine(
                $"{Path.GetFileNameWithoutExtension(acbfile.FullName)} - 唤起失败!请前往\"https://vgmstream.org/\"下载Command-line (64-bit) Win版本后解压至软件目录.");
            return;
        }

        var vgmStreamCommands =
            $"{awbfilename} -S 0 -o {OutFolder}\\?n.wav";
        var process = new Process
        {
            StartInfo =
            {
                FileName = path + @"\vgmstream-cli.exe",
                Arguments = vgmStreamCommands,
                UseShellExecute = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();
        Console.WriteLine($"{Path.GetFileNameWithoutExtension(acbfile.FullName)} - 使用vgmstream工具解包完成.");
        File.Delete(acbfile.FullName);
        File.Delete(awbfilename);
        Directory.Delete(destinationFolder.FullName, true);
    }

    public static void DecodeUsmFiles(FileInfo filename)
    {
        var volume = 1F;
        var mode = 16;
        var loop = 0;
        var ciphKey1 = 0x92EBF464;
        uint ciphKey2 = 0x7E896318;
        var path = Directory.GetCurrentDirectory();
        var cridCommands = " -a 92EBF464 -b 7E896318 -v -n \"" +
                           filename.FullName + "\"";
        Console.WriteLine(filename.Name + " - 解密m2v文件...");
        var process = new Process
        {
            StartInfo =
            {
                FileName = path + @"\crid.exe",
                Arguments = cridCommands,
                UseShellExecute = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();
        var af = new CriUsmStream(filename.FullName);
        var option = new MpegStream.DemuxOptionsStruct
        {
            ExtractAudio = true,
            SplitAudioStreams = true
        };
        af.DemultiplexStreams(option);
        foreach (var hcafile in new DirectoryInfo(filename.DirectoryName).GetFiles("*.bin",
                     SearchOption.AllDirectories))
        {
            if (!hcafile.Name.Contains(filename.Name.Substring(0, filename.Name.Length - 4))) continue;
            using (var inputFileStream = File.Open(hcafile.FullName, FileMode.Open, FileAccess.Read))
            {
                Console.WriteLine(hcafile.Name + " - 解密hca...");
                using (var outputFileStream =
                       File.Open(
                           filename.DirectoryName + @"\" + filename.Name.Substring(0, filename.Name.Length - 4) +
                           @".demux\" + hcafile.Name.Substring(0, hcafile.Name.Length - 4) + ".wav",
                           FileMode.Create, FileAccess.Write))
                {
                    var decodeParams = DecodeParams.CreateDefault();
                    decodeParams.Key1 = ciphKey1;
                    decodeParams.Key2 = ciphKey2;
                    decodeParams.KeyModifier = 0;

                    var audioParams = AudioParams.CreateDefault();

                    audioParams.InfiniteLoop = AudioParams.Default.InfiniteLoop;
                    audioParams.SimulatedLoopCount = AudioParams.Default.SimulatedLoopCount;
                    audioParams.OutputWaveHeader = true;

                    using (var hcaStream = new HcaAudioStream(inputFileStream, decodeParams, audioParams))
                    {
                        var read = 1;
                        var dataBuffer = new byte[1024];

                        while (read > 0)
                        {
                            read = hcaStream.Read(dataBuffer, 0, dataBuffer.Length);

                            if (read > 0) outputFileStream.Write(dataBuffer, 0, read);
                        }
                    }
                }
            }

            File.Delete(hcafile.FullName);
        }

        var m2vfiles = new DirectoryInfo(filename.DirectoryName + @"\" +
                                         filename.Name.Substring(0, filename.Name.Length - 4) + @".demux\")
            .GetFiles("*.m2v", SearchOption.AllDirectories);
        var m2vfile = m2vfiles[0];
        var wavfiles = new DirectoryInfo(filename.DirectoryName + @"\" +
                                         filename.Name.Substring(0, filename.Name.Length - 4) + @".demux\")
            .GetFiles("*.wav", SearchOption.AllDirectories);
        var wavefile = wavfiles[0];
        var ffmpegCommands = "-i " + "\"" + m2vfile.FullName + "\"" + " -i " + "\"" + wavefile.FullName + "\"" +
                             " -c:v copy -c:a aac -strict experimental " + "\"" + filename.DirectoryName + @"\" +
                             filename.Name.Substring(0, filename.Name.Length - 4) + @".demux\" +
                             filename.Name.Substring(0, filename.Name.Length - 4) + "_final.mp4" + "\"";
        Console.WriteLine(filename.Name + " - 合并m2v、wav文件...");
        var process2 = new Process
        {
            StartInfo =
            {
                FileName = path + @"\ffmpeg.exe",
                Arguments = ffmpegCommands,
                UseShellExecute = true,
                CreateNoWindow = true
            }
        };
        process2.Start();
        process2.WaitForExit();
    }

    public static void DecodeBGOSpecialUsmFiles(FileInfo filename)
    {
        var volume = 1F;
        var mode = 16;
        var loop = 0;
        var ciphKey1 = 0x92EBF464;
        uint ciphKey2 = 0x7E896318;
        var path = Directory.GetCurrentDirectory();
        var cridCommands = " -a 92EBF464 -b 7E896318 -v -n -c \"" +
                           filename.FullName + "\"";
        Console.WriteLine(filename.Name + " - 解密m2v文件...");
        var process = new Process
        {
            StartInfo =
            {
                FileName = path + @"\crid.exe",
                Arguments = cridCommands,
                UseShellExecute = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();
        var m2vfiles = new DirectoryInfo(filename.DirectoryName + @"\" +
                                         filename.Name.Substring(0, filename.Name.Length - 4) + @".demux\")
            .GetFiles("*.m2v", SearchOption.AllDirectories);
        var m2vfile = m2vfiles[0];
        var wavfiles = new DirectoryInfo(filename.DirectoryName + @"\" +
                                         filename.Name.Substring(0, filename.Name.Length - 4) + @".demux\")
            .GetFiles("*.wav", SearchOption.AllDirectories);
        var wavefile = wavfiles[0];
        var ffmpegCommands = "-i " + "\"" + m2vfile.FullName + "\"" + " -i " + "\"" + wavefile.FullName + "\"" +
                             " -c:v libx264 -c:a aac -strict experimental " + "\"" + filename.DirectoryName + @"\" +
                             filename.Name.Substring(0, filename.Name.Length - 4) + @".demux\" +
                             filename.Name.Substring(0, filename.Name.Length - 4) + "_final.mp4" + "\"";
        Console.WriteLine(filename.Name + " - 合并m2v、wav文件...");
        var process2 = new Process
        {
            StartInfo =
            {
                FileName = path + @"\ffmpeg.exe",
                Arguments = ffmpegCommands,
                UseShellExecute = true,
                CreateNoWindow = true
            }
        };
        process2.Start();
        process2.WaitForExit();
    }
}