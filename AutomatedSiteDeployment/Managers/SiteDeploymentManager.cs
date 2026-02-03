using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutomatedSiteDeployment.Helpers;
using AutomatedSiteDeployment.Models;
using Models.Shared.Models.Domains;
using AutomatedSiteDeployment.Agents;

namespace AutomatedSiteDeployment.Managers
{
    internal class SiteDeploymentManager
    {
        private Dictionary<string, string> fileCopyList;
        private readonly string domainName;
        private readonly string stgPath;
        private readonly string livePath;

        private string sourceDirectory = string.Empty;
        private string destinationDirectory = string.Empty;
        private string backupDirectory = string.Empty;

        private bool backedUp = false;

        private readonly Users stgUser;
        private readonly Users liveUser;

        private readonly FileSystemAgent sourceWorker = null; // Reader
        private readonly FileSystemAgent destinationWorker = null; // Writer

        private readonly List<string> RestrictedFileContainsList = new List<string>
        {
            "_ignore_me_", "_error_"
        };

        public string message = string.Empty;
        public bool deploySuccess;

        public SiteDeploymentManager(Domain domain, SettingsHelper settings)
        {
            try
            {
                fileCopyList = new Dictionary<string, string>();
                domainName = domain.DomainName;
                stgPath = settings._stgPath;
                stgUser = new Users()
                {
                    Username = settings._stgUsername,
                    Password = settings._stgPassword,
                    Server = "AZstg"
                };
                var liveServer = domain.HostedSiteDetails?.ServerName;
                if (liveServer == "AZ102")
                {
                    livePath = settings._AZ102Path;
                    liveUser = new Users()
                    {
                        Username = settings._AZ102Username,
                        Password = settings._AZ102Password,
                        Server = "AZ102"
                    };
                }
                else if (liveServer == "AZ201")
                {
                    livePath = settings._AZ201Path;
                    liveUser = new Users()
                    {
                        Username = settings._AZ201Username,
                        Password = settings._AZ201Password,
                        Server = "AZ201"
                    };
                }
                else
                {
                    message = $"Unsupported live server: {liveServer}";
                    deploySuccess = false;
                    return;
                }

                // Initialize our producer and consumer workers
                sourceWorker = new FileSystemAgent(stgUser.Username, stgUser.Password, stgUser.Server);
                destinationWorker = new FileSystemAgent(liveUser.Username, liveUser.Password, liveUser.Server);

                // Define source and destination directories
                sourceDirectory = Path.Combine(stgPath, domainName);
                destinationDirectory = Path.Combine(livePath, domainName);
                backupDirectory = Path.Combine(livePath, "Rollback", $"temp-{domainName}");
            }
            catch (Exception ex)
            {
                message = $"Error initializing SiteDeploymentManager: {ex.Message}";
                deploySuccess = false;

                if (sourceWorker != null)
                {
                    sourceWorker.Stop();
                }
                if (destinationWorker != null)
                {
                    destinationWorker.Stop();
                }
            }
        }

        public async Task StartDeployment()
        {
            try
            {
                if (!sourceWorker.DirectoryExists(sourceDirectory))
                {
                    message = $"Source directory does not exist: {sourceDirectory}";
                    deploySuccess = false;
                    return;
                }

                if (!destinationWorker.DirectoryExists(destinationDirectory))
                {
                    destinationWorker.CreateDirectory(destinationDirectory);
                }

                if (destinationWorker.DirectoryExists(backupDirectory))
                {
                    destinationWorker.DeleteDirectory(backupDirectory);
                }

                destinationWorker.CreateDirectory(backupDirectory);

                // Backup existing files
                await Task.Run(() => BackupDestinationSite());

                // Generate list of files to copy
                await Task.Run(() => GenerateFileCopyList());

                // Deploy files
                await Task.Run(() => Deploy());

                deploySuccess = true;
            }
            catch (Exception ex)
            {
                message = $"Error during site deployment: {ex.Message}";
                deploySuccess = false;

                if (backedUp)
                {
                    await Task.Run(() => RestoreFromBackup());
                }
            }
        }

        private void BackupDestinationSite()
        {
            var files = destinationWorker.GetFiles(destinationDirectory, "*", SearchOption.AllDirectories);

            if (files == null || files.Length == 0)
            {
                Console.WriteLine("No files found in the destination directory to back up.");
                return;
            }

            foreach (var file in files)
            {
                try
                {
                    var relativePath = Path.GetRelativePath(destinationDirectory, file);
                    var backupFilePath = Path.Combine(backupDirectory, relativePath);
                    var backupFileDir = Path.GetDirectoryName(backupFilePath);
                    if (!destinationWorker.DirectoryExists(backupFileDir))
                    {
                        destinationWorker.CreateDirectory(backupFileDir);
                    }

                    // backup directory will live on the destination server so we can simply copy the file over
                    destinationWorker.CopyFile(file, backupFilePath, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error backing up file {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            Console.WriteLine("Backup completed successfully.");
            backedUp = true;
        }

        private void RestoreFromBackup()
        {
            var backupFiles = destinationWorker.GetFiles(backupDirectory, "*", SearchOption.AllDirectories);
            if (backupFiles == null || backupFiles.Length == 0)
            {
                Console.WriteLine("No files found in the backup directory to restore.");
                return;
            }
            foreach (var file in backupFiles)
            {
                try
                {
                    var relativePath = Path.GetRelativePath(backupDirectory, file);
                    var restoreFilePath = Path.Combine(destinationDirectory, relativePath);
                    var restoreFileDir = Path.GetDirectoryName(restoreFilePath);
                    if (!destinationWorker.DirectoryExists(restoreFileDir))
                    {
                        destinationWorker.CreateDirectory(restoreFileDir);
                    }
                    destinationWorker.CopyFile(file, restoreFilePath, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error restoring file {Path.GetFileName(file)}: {ex.Message}");
                }
            }
            Console.WriteLine("Restoration from backup completed successfully.");
        }

        private void GenerateFileCopyList()
        {
            try
            {
                fileCopyList.Clear();
                var sourceFiles = sourceWorker.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories);
                foreach (var file in sourceFiles)
                {
                    var relativePath = Path.GetRelativePath(sourceDirectory, file);
                    var destFilePath = Path.Combine(destinationDirectory, relativePath);
                    // Check for restricted file names
                    if (RestrictedFileContainsList.Any(restricted => relativePath.ToUpper().Contains(restricted)))
                    {
                        continue; // Skip this file
                    }
                    fileCopyList[file] = destFilePath;
                }
            }
            catch (Exception ex)
            {
                message += $"Error generating file copy list: {ex.Message}. ";
                throw;
            }
        }

        private void Deploy()
        {
            if (fileCopyList.Count == 0)
            {
                Console.WriteLine("No files to deploy.");
                return;
            }
            try
            {
                Console.WriteLine("Starting file deployment...");
                foreach (var destPath in fileCopyList.Values)
                {
                    var destDir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(destDir) && !destinationWorker.DirectoryExists(destDir))
                    {
                        destinationWorker.CreateDirectory(destDir);
                    }
                }

                Console.WriteLine("Directory structure created. Copying files...");
                foreach (var file in fileCopyList)
                {
                    var sourceFile = file.Key;
                    var destFile = file.Value;
                    var fileName = Path.GetFileName(sourceFile);

                    try
                    {
                        const int bufferSize = 65536;
                        using (Stream sourceStream = sourceWorker.OpenRead(sourceFile))
                        using (Stream destinationStream = destinationWorker.OpenWrite(destFile))
                        {
                            byte[] buffer = new byte[bufferSize];
                            int bytesRead;
                            do
                            {
                                bytesRead = sourceStream.Read(buffer, 0, bufferSize);
                                if (bytesRead > 0)
                                {
                                    destinationStream.Write(buffer, 0, bytesRead);
                                }
                            } while (bytesRead > 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error copying file {sourceFile}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                message += $"Error during deployment: {ex.Message}. ";
                throw;
            }
        }

    }
}
