using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace WebApplication9.Comm
{

    public class SaveFile
    {
        private String _ftpServerIP;
        private String _httpDomain;
        private String _ftpUserID;
        private String _ftpPassword;

        public SaveFile(string ftpServerIP, string httpDomian, string ftpUserID, string ftpPassword)
        {
            this._ftpServerIP = ftpServerIP;
            this._httpDomain = httpDomian;
            this._ftpUserID = ftpUserID;
            this._ftpPassword = ftpPassword;
        }

        public SaveFile() { }

        //创建本地文件夹
        private static void CreateDirectoryLocal(string targetDir)
        {
            DirectoryInfo dir = new DirectoryInfo(targetDir);
            if (!dir.Exists)
                dir.Create();
        }

        //创建ftp文件夹
        public bool CreateDirectoryFtp(string fpath)
        {
            FtpWebRequest reqFTP;
            try
            {
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(fpath));
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(this._ftpUserID, this._ftpPassword);
                reqFTP.Method = WebRequestMethods.Ftp.MakeDirectory;
                WebResponse response = reqFTP.GetResponse();
                return true;
            }
            catch
            {
                return false;
            }
        }

        //判断是否是文件夹
        /// <summary>
        /// 目录是否存在
        /// </summary>
        /// <param name="fpath"></param>
        /// <returns></returns>
        public bool IsDir(string fpath)
        {
            FtpWebRequest reqFTP;
            try
            {
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(fpath));
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(this._ftpUserID, this._ftpPassword);
                reqFTP.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                WebResponse response = reqFTP.GetResponse();
                Stream responseStream = response.GetResponseStream();
                StreamReader readStream = new StreamReader(responseStream, System.Text.Encoding.Default);
                string aa = readStream.ReadToEnd();
                if (aa == "") return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        //删除ftp文件
        public bool DeleteFtp(string fpath)
        {
            Regex re = new Regex(_httpDomain);
            fpath = re.Replace(fpath, "ftp://" + _ftpServerIP, 1);
            FtpWebRequest reqFTP;
            try
            {
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(fpath));
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(_ftpUserID, _ftpPassword);
                reqFTP.Method = WebRequestMethods.Ftp.DeleteFile;
                WebResponse response = reqFTP.GetResponse();
                return true;
            }
            catch
            {
                return false;
            }
        }

        //删除本地文件
        public void DeleteLocal(string fileName)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
        }

        //获取文件的全路径(如果是根目录则会出现问题)
        public string GetFileFullName(string directory, string controlPath, string extendName, bool isWeb)
        {
            string url = "";
            string month = DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString();
            if (isWeb)
            {
                if (string.IsNullOrEmpty(controlPath))
                {
                    url = String.Format(directory + "/{0}/{1}", DateTime.Now.ToString("yyyyMMddhhmmssfff") + RandomUtil.CreateRandom(3), extendName);
                }
               
            }
           
            return "ftp://" + _ftpServerIP + url;
        }

        //保存到ftp服务器上
        public string SaveFtp(Stream stream, string fileName)
        {
            string uripath = fileName.Remove(fileName.LastIndexOf('/'));
            string url = "";
            if (!IsDir(uripath)) CreateDirectoryFtp(uripath);
            //if (!IsDir(this._ftpuri)) CreateDir(this._ftpuri);

            // 根据uri创建FtpWebRequest对象 
            FtpWebRequest reqFTP;
            reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(fileName));

            // ftp用户名和密码 
            reqFTP.Credentials = new NetworkCredential(this._ftpUserID, this._ftpPassword);

            // 默认为true，连接不会被关闭 
            // 在一个命令之后被执行 
            reqFTP.KeepAlive = false;

            // 指定执行什么命令 
            reqFTP.Method = WebRequestMethods.Ftp.UploadFile;

            // 指定数据传输类型 
            reqFTP.UseBinary = true;

            // 上传文件时通知服务器文件的大小 
            reqFTP.ContentLength = stream.Length;

            // 缓冲大小设置为2kb 
            int buffLength = 1024;
            byte[] buff = new byte[buffLength];
            int contentLen;

            try
            {
                // 把上传的文件写入流 
                Stream strm = reqFTP.GetRequestStream();

                // 每次读文件流的1kb                 
                contentLen = stream.Read(buff, 0, buffLength);
                while (contentLen != 0)
                {
                    // 把内容从file stream 写入 upload stream 
                    strm.Write(buff, 0, contentLen);
                    contentLen = stream.Read(buff, 0, buffLength);
                }
                stream.Close();
                strm.Close();
                url = fileName.Replace(String.Format("ftp://{0}", this._ftpServerIP), _httpDomain);
            }
            catch
            {
                url = String.Empty;
            }
            return url;
        }
        public Stream LoadFtp(string filePath)
        {
            //FileStream fs = null;
            Stream responseStream = null;
            filePath = filePath.Replace(_httpDomain, "ftp://" + _ftpServerIP);
            try
            {
                //创建一个与FTP服务器联系的FtpWebRequest对象
                FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(filePath);
                //设置请求的方法是FTP文件下载
                request.Method = WebRequestMethods.Ftp.DownloadFile;

                //连接登录FTP服务器
                request.Credentials = new NetworkCredential(_ftpUserID, _ftpPassword);

                //获取一个请求响应对象
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                //获取请求的响应流
                responseStream = response.GetResponseStream();
            }
            catch (Exception e)
            {
                responseStream = null;
            }
            return responseStream;
        }
        public void LoadFtp(string filePath, string fileName)
        {
            FileStream fs = null;
            Stream responseStream = null;
            filePath = filePath.Replace(_httpDomain, "ftp://" + _ftpServerIP);
            try
            {
                //创建一个与FTP服务器联系的FtpWebRequest对象
                FtpWebRequest request = (FtpWebRequest)FtpWebRequest.Create(filePath);
                //设置请求的方法是FTP文件下载
                request.Method = WebRequestMethods.Ftp.DownloadFile;

                //连接登录FTP服务器
                request.Credentials = new NetworkCredential(_ftpUserID, _ftpPassword);

                //获取一个请求响应对象
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                //获取请求的响应流
                responseStream = response.GetResponseStream();

                //判断本地文件是否存在，如果存在，则打开和重写本地文件
                if (File.Exists(fileName))
                {
                    fs = File.Open(fileName, FileMode.Open, FileAccess.ReadWrite);
                }
                //判断本地文件是否存在，如果不存在，则创建本地文件
                else
                {
                    CreateDirectoryLocal(Path.GetDirectoryName(fileName));
                    fs = File.Create(fileName);
                }

                if (fs != null)
                {

                    int buffer_count = 1024;
                    byte[] buffer = new byte[buffer_count];
                    int size = 0;
                    while ((size = responseStream.Read(buffer, 0, buffer_count)) > 0)
                    {
                        fs.Write(buffer, 0, size);
                    }
                    fs.Flush();
                    fs.Close();
                    responseStream.Close();
                }
            }
            finally
            {
                if (fs != null)
                    fs.Close();
                if (responseStream != null)
                    responseStream.Close();
            }

        }
        //保存到本地
        public string SaveLocal(Stream stream, string fileName)
        {
            string url = "";
            try
            {
                CreateDirectoryLocal(Path.GetDirectoryName(fileName));
                using (FileStream fileStream = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    byte[] buffer = new byte[1024 * 10];
                    int contentLength = stream.Read(buffer, 0, buffer.Length);
                    while (contentLength != 0)
                    {
                        fileStream.Write(buffer, 0, contentLength);
                        contentLength = stream.Read(buffer, 0, buffer.Length);
                    }
                    stream.Close();
                    fileStream.Close();
                    url = fileName;
                }
            }
            catch (Exception)
            {

                url = string.Empty;
            }
            return url;

        }
        //UE S
        //获取文件的全路径(如果是根目录则会出现问题)
        public string GetFileFullNameUE(string directory, string controlPath, string extendName, bool isWeb)
        {
            string url = "";
            string realname = System.Guid.NewGuid().ToString();
            if (isWeb)
            {
                if (string.IsNullOrEmpty(controlPath))
                {
                    url = String.Format(directory + "/{0}{1}", realname, extendName);
                }
                else
                {
                    url = String.Format(directory + "/{0}/{1}{2}", controlPath, realname, extendName);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(controlPath))
                {
                    url = String.Format(directory + "\\{0}\\{1}{2}", realname, DateTime.Now.ToString("yyyyMMddhhmmssfff"), extendName);
                }
                else
                {
                    url = String.Format(directory + "\\{0}\\{1}\\{2}{3}", controlPath, realname, DateTime.Now.ToString("yyyyMMddhhmmssfff"), extendName);
                }
            }
            return url;
        }
        //UE E
    }
    public sealed class RandomUtil
    {
        /// <summary>
        /// 生成可变长[0-9]随机数
        /// </summary>
        /// <param name="length">随机数的长度[默认6位]</param>
        /// <returns>返回随机数字符串</returns>
        public static string CreateRandom(int length = 6)
        {
            Random rd = new Random();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                int num = rd.Next(0, 10);
                sb.Append(num);
            }
            return sb.ToString();
        }
    }
}