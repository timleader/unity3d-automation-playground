
using System;
using System.Text;
using System.IO;

using ICSharpCode.SharpZipLib.Zip;

using Automation.Common;

namespace Automation.Runtime
{

    public static class FileWithJarSupport
    {

        //-------------------------------------------------------------------------
        private const string LogChannel = "io.filewithjarsupport";
        private const string JAR_PROTOCOL = "jar:file://";
        
        #region PUBLIC_INTERFACE 

        //-------------------------------------------------------------------------
        public static bool IsJarFilepath(string path)
        { 
            return path.StartsWith(JAR_PROTOCOL);
        }

        //-------------------------------------------------------------------------
        public static bool Exists(string path)
        {
            if (path.StartsWith(JAR_PROTOCOL))
            {
                path = path.Substring(JAR_PROTOCOL.Length);
                string[] pathSegments = path.Split('!');

                if (pathSegments.Length == 1)
                {
                    return File.Exists(path);
                }
                else if (pathSegments.Length <= 2)
                {
                    string zipPath = pathSegments[0];
                    string entryPath = pathSegments[1].TrimStart('/');

                    ZipFile zipFile = null;

                    try
                    {
                        if (File.Exists(zipPath) == true)
                        {
                            using (var fileStream = File.OpenRead(zipPath))
                            {
                                zipFile = new ZipFile(fileStream);
                                ZipEntry entry = zipFile.GetEntry(entryPath);
                                return entry != null;
                            }
                        }
                        else
                        {
                            Logger.TraceFormat(LogChannel, "zipPath exists=false: '{0}'", zipPath);
                            return false;
                        }
                    }
                    catch (Exception exception)
                    {
                        Logger.Exception(LogChannel, exception);
                        throw exception;
                    }
                    finally
                    {
                        if (zipFile != null)
                        {
                            zipFile.IsStreamOwner = true;
                            zipFile.Close();
                        }
                    }
                }
                else
                {
                    throw new ArgumentException();
                }
            }
            else
            {
                return File.Exists(path);
            }
        }

        //-------------------------------------------------------------------------
        public static byte[] ReadAllBytes(string path)
        {
            if (path.StartsWith(JAR_PROTOCOL))
            {
                path = path.Substring(JAR_PROTOCOL.Length);
                string[] pathSegments = path.Split('!');

                if (pathSegments.Length == 1)
                {
                    return File.ReadAllBytes(path);
                }
                else if (pathSegments.Length <= 2)
                {
                    string zipPath = pathSegments[0];
                    string entryPath = pathSegments[1].TrimStart('/');

                    ZipFile zipFile = null;

                    try
                    {
                        if (File.Exists(zipPath) == true)
                        {
                            zipFile = new ZipFile(File.OpenRead(zipPath));
                            ZipEntry entry = zipFile.GetEntry(entryPath);
                            if (entry != null)
                            {
                                byte[] data = new byte[entry.Size];

                                Stream zipStream = zipFile.GetInputStream(entry);
                                zipStream.Read(data, 0, data.Length);

                                return data;
                            }
                            else
                            {
                                throw new FileNotFoundException();
                            }
                        }
                        else
                        {
                            throw new FileNotFoundException();
                        }
                    }
                    catch (Exception exception)
                    {
                        throw exception;
                    }
                    finally
                    {
                        if (zipFile != null)
                        {
                            zipFile.IsStreamOwner = true;
                            zipFile.Close();
                        }
                    }
                }
                else
                {
                    throw new ArgumentException();
                }
            }
            else
            {
                return File.ReadAllBytes(path);
            }
        }

        //-------------------------------------------------------------------------
        public static string ReadAllText(string path)
        {
            return ReadAllText(path, Encoding.Default);
        }

        //-------------------------------------------------------------------------
        public static string ReadAllText(string path, Encoding encoding)
        {
            byte[] data = ReadAllBytes(path);
            string text = encoding.GetString(data);
            return text;
        }


        #endregion
        
    }

}
 