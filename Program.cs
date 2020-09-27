using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CleanGooglePhotosTakeOut
{
    class Program
    {
        static string[] ExtensionsKnowed = {".json", ".jpg", ".mp4", ".jpeg", ".heic", ".gif", ".png"};
        static StreamWriter logWriter = null;
        
        static void Main(string[] args)
        {
            InitLog();
            var pathToClean = @"C:\GooglePhotos";            
            WriteLine($"Reading path: {pathToClean}");
            var dirInfo = new System.IO.DirectoryInfo(pathToClean);
            var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
            FixFileNamesWithExtensionFinishInNumber(files);

            files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
            
            
            WriteInfoProcess(dirInfo);

            //Remove json files
            RemoveJsonFiles(files);

            //Remove duplicate files
            RemoveDuplicateFiles(files);

            // remove folder empty
            RemoveEmptyFolders(dirInfo);

            //Check again
            WriteLine($"======================================= ");
            WriteLine($"============= Check process =========== ");
            WriteInfoProcess(dirInfo, true);

            logWriter.Dispose();
        }

        private static void RemoveJsonFiles(FileInfo[] files) {
            var countRemovedJson = 0;
            WriteLine();
            WriteLine("Removing json files:");
            foreach (var file in files.Where(z=>z.Extension.ToLower() == ".json")) {
                DeleteFile(file, writeLog:false);
                countRemovedJson++;
            }
            WriteLine($"\nJson deleted: " + countRemovedJson, ConsoleColor.Green);

        }

        private static void RemoveDuplicateFiles(FileInfo[] files) {
            var duplicates = GetDuplicateFiles(files);

            WriteLine();
            WriteLine("Removing duplicated files:");
            Regex regexTimelineOpt1 = new Regex(@"^(19[5-9][0-9]|20[0-4][0-9]|2050)-([0-1][0-9])-([0-3][0-9])$");   //yyyy-mm-dd 
            Regex regexTimelineOpt2 = new Regex(@"^(19[5-9][0-9]|20[0-4][0-9]|2050)-([0-1][0-9])-([0-3][0-9])-([0-3][0-9])$");   //yyyy-mm-dd-dd 
            Regex regexTimelineOpt3 = new Regex(@"^(19[5-9][0-9]|20[0-4][0-9]|2050)-([0-1][0-9])-([0-3][0-9]) - ([0-1][1-9])-([0-3][0-9])$");   //yyyy-mm-dd - mm-dd
            Regex regexTimelineOpt4 = new Regex(@"^(19[5-9][0-9]|20[0-4][0-9]|2050)-([0-1][0-9])-([0-3][0-9]) (#[1-9])$");   //yyyy-mm-dd #2 
            var countRemovedDuplicate = 0;

            foreach (var file in duplicates) {
                var nameParentDir = file.Directory.Name;
                if(regexTimelineOpt1.IsMatch(nameParentDir) 
                    || regexTimelineOpt2.IsMatch(nameParentDir)
                    || regexTimelineOpt3.IsMatch(nameParentDir)
                    || regexTimelineOpt4.IsMatch(nameParentDir))
                {
                    // folder timeline
                    var theDuplicates= files.Where(f => f.Name == file.Name && f.FullName != file.FullName);
                    var existInAlbum = theDuplicates.Any( d => !regexTimelineOpt1.IsMatch(d.Directory.Name) 
                                                            && !regexTimelineOpt2.IsMatch(d.Directory.Name)
                                                            && !regexTimelineOpt3.IsMatch(d.Directory.Name)
                                                            && !regexTimelineOpt4.IsMatch(d.Directory.Name));                                                            
                    
                    if(existInAlbum){
                        DeleteFile(file, "Exist in album - ");
                        countRemovedDuplicate++;
                    }
                    else
                        WriteLine("NOT Exist in album: " + file.Name, ConsoleColor.Red);
                }
                else 
                {
                    // file in album folder no delete
                }
            }
            WriteLine($"\nDuplicate deleted: " + countRemovedDuplicate, ConsoleColor.Green);
        }

        private static void RemoveEmptyFolders (DirectoryInfo dirInfo){
            var foldersEmpty = dirInfo.GetDirectories().Where(d => !d.EnumerateFiles().Any());
            WriteLine($"\nEmpty folders to remove: " + foldersEmpty.Count(), ConsoleColor.Yellow);
            foreach (var folder in foldersEmpty) {
                DeleteFolder(folder);
                WriteLine("Empty folder removed: " + folder.Name);
            }
        }

        private static void WriteInfoProcess(DirectoryInfo dirInfo, bool logDuplicates = false){
            var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
            var duplicates = GetDuplicateFiles(files);
            WriteLine($"Path: {dirInfo.FullName}");
            WriteLine($"Total files: \t\t" + files.Count());
            WriteLine($"Total .json files: \t" + files.Count(f=>f.Extension.ToLower() == ".json" ));
            WriteLine($"Total .jpg files: \t" + files.Count(f=>f.Extension.ToLower() == ".jpg" || f.Extension.ToLower() == ".jpeg"));
            WriteLine($"Total .gif  files: \t" + files.Count(f=>f.Extension.ToLower() == ".gif"));
            WriteLine($"Total .png  files: \t" + files.Count(f=>f.Extension.ToLower() == ".png"));
            WriteLine($"Total .heic  files: \t" + files.Count(f=>f.Extension.ToLower() == ".heic"));
            WriteLine($"Total .mov  files: \t" + files.Count(f=>f.Extension.ToLower() == ".mov"));
            WriteLine($"Total .mp4  files: \t" + files.Count(f=>f.Extension.ToLower() == ".mp4"));
            WriteLine($"Total other files: \t" + files.Count(f=> !ExtensionsKnowed.Contains(f.Extension.ToLower())));
            WriteLine($"Duplicate files: \t" + duplicates.Count(), duplicates.Count() > 0 ? ConsoleColor.Red : ConsoleColor.Green);
            WriteLine();
            WriteLine("Extensions:");
            foreach (var extension in files.Select(z=>z.Extension).Distinct()) {
                WriteLine(extension);    
            }            

            if(logDuplicates){
                WriteToFileLog("List duplicate files: ");
                foreach (var file in duplicates)
                    WriteToFileLog(file.FullName);
            }
                
        }

        private static void WriteLine (string text = "", ConsoleColor color = ConsoleColor.White){
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            WriteToFileLog(text);
        }
        
        private static void DeleteFile(FileInfo file, string customMessage = null, bool writeLog = true){
            if(file.Exists){
                file.Delete();
                if (writeLog)
                    WriteToFileLog($"{customMessage}File deleted: {file.FullName}");
            }
        }
        private static void DeleteFolder(DirectoryInfo folder){
            if(folder.Exists){
                folder.Delete();
                WriteToFileLog($"folder deleted: {folder.FullName}");
            }
        }

        private static void InitLog()
        {
            var logPath = System.IO.Directory.GetCurrentDirectory() + $"\\CleanGooglePhotosTakeOut"+ DateTime.Now.ToString("yyyyMMdd_HHmmss") +".log";
            var logFile = System.IO.File.Create(logPath);
            logWriter = new System.IO.StreamWriter(logFile);
        }

        private static void WriteToFileLog(string text){
            logWriter.WriteLine(text);
        }

        private static void FixFileNamesWithExtensionFinishInNumber(FileInfo[] files){
            Regex regexDotNumber = new Regex(@"^.([0-9][0-9])$");   //.05
            WriteLine("Checking extension files to be fixed...");
            foreach (var file in files)
            {
                if(regexDotNumber.IsMatch(file.Extension)){
                    WriteLine($"File extension fixed: {file.Name} to {file.Name}.jpg");
                    var newFileName = file.FullName + ".jpg";
                    if(File.Exists(newFileName)) File.Delete(newFileName);
                    file.MoveTo(newFileName);
                }    
            }
        }
        
        private static FileInfo[] GetDuplicateFiles(FileInfo[] files){
            return files.Where(f => f.Extension.ToLower() != ".json")
                        .GroupBy(c => c.Name)
                        .Where(g => g.Skip(1).Any())
                        .SelectMany(c => c)
                        .ToArray();
        }

    }
}

