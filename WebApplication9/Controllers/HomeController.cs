using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication9.Comm;

namespace WebApplication9.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/
        public ActionResult Index()
        {
            return View();
        }


        public JsonResult Upload(HttpPostedFileBase fileData)
        {
            if (fileData != null)
            {
                try
                {
                    // 文件上传后的保存路径
                    string filePath = Server.MapPath("~/Uploads/");
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }
                    string fileName = Path.GetFileName(fileData.FileName);// 原始文件名称
                    string fileExtension = Path.GetExtension(fileName); // 文件扩展名
                    string saveName = Guid.NewGuid().ToString() + fileExtension; // 保存文件名称

                    fileData.SaveAs(filePath + saveName);


                    SaveFile saveFile = new SaveFile(ConfigurationManager.AppSettings["ftpip"], "", ConfigurationManager.AppSettings["ftpuser"], ConfigurationManager.AppSettings["ftppass"]);
                    string fullName = saveFile.GetFileFullName("", "", fileName, true);
                    string fileUrl = saveFile.SaveFtp(fileData.InputStream, fullName);

                    return Json(new { Success = true, FileName = fileName, SaveName = filePath + saveName });
                }
                catch (Exception ex)
                {
                    return Json(new { Success = false, Message = ex.Message }, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {

                return Json(new { Success = false, Message = "请选择要上传的文件！" }, JsonRequestBehavior.AllowGet);
            }
        }


        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="fileinfo">需要上传的文件</param>
        /// <param name="targetDir">目标路径</param>
        /// <param name="hostname">ftp地址</param>
        /// <param name="username">ftp用户名</param>
        /// <param name="password">ftp密码</param>
        public static void UploadFile(FileInfo fileinfo, string targetDir, string hostname, string username, string password)
        {
            //1. check target
            string target;
            if (targetDir.Trim() == "")
            {
                return;
            }
            target = Guid.NewGuid().ToString();  //使用临时文件名


            string URI = "FTP://" + hostname + "/" + targetDir + "/" + target;
            ///WebClient webcl = new WebClient();
            System.Net.FtpWebRequest ftp = GetRequest(URI, username, password);

            //设置FTP命令 设置所要执行的FTP命令，
            //ftp.Method = System.Net.WebRequestMethods.Ftp.ListDirectoryDetails;//假设此处为显示指定路径下的文件列表
            ftp.Method = System.Net.WebRequestMethods.Ftp.UploadFile;
            //指定文件传输的数据类型
            ftp.UseBinary = true;
            ftp.UsePassive = true;

            //告诉ftp文件大小
            ftp.ContentLength = fileinfo.Length;
            //缓冲大小设置为2KB
            const int BufferSize = 2048;
            byte[] content = new byte[BufferSize - 1 + 1];
            int dataRead;

            //打开一个文件流 (System.IO.FileStream) 去读上传的文件
            using (FileStream fs = fileinfo.OpenRead())
            {
                try
                {
                    //把上传的文件写入流
                    using (Stream rs = ftp.GetRequestStream())
                    {
                        do
                        {
                            //每次读文件流的2KB
                            dataRead = fs.Read(content, 0, BufferSize);
                            rs.Write(content, 0, dataRead);
                        } while (!(dataRead < BufferSize));
                        rs.Close();
                    }

                }
                catch (Exception ex) { }
                finally
                {
                    fs.Close();
                }

            }

            ftp = null;
            //设置FTP命令
            ftp = GetRequest(URI, username, password);
            ftp.Method = System.Net.WebRequestMethods.Ftp.Rename; //改名
            ftp.RenameTo = fileinfo.Name;
            try
            {
                ftp.GetResponse();
            }
            catch (Exception ex)
            {
                ftp = GetRequest(URI, username, password);
                ftp.Method = System.Net.WebRequestMethods.Ftp.DeleteFile; //删除
                ftp.GetResponse();
                throw ex;
            }
            finally
            {
                //fileinfo.Delete();
            }

            // 可以记录一个日志  "上传" + fileinfo.FullName + "上传到" + "FTP://" + hostname + "/" + targetDir + "/" + fileinfo.Name + "成功." );
            ftp = null;

            #region
            /*****
             *FtpWebResponse
             * ****/
            //FtpWebResponse ftpWebResponse = (FtpWebResponse)ftp.GetResponse();
            #endregion
        }
        private static FtpWebRequest GetRequest(string URI, string username, string password)
        {
            //根据服务器信息FtpWebRequest创建类的对象
            FtpWebRequest result = (FtpWebRequest)FtpWebRequest.Create(URI);
            //提供身份验证信息
            result.Credentials = new System.Net.NetworkCredential(username, password);
            //设置请求完成之后是否保持到FTP服务器的控制连接，默认值为true
            result.KeepAlive = false;
            return result;
        }
    }
}