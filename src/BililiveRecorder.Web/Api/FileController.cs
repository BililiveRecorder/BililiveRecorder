using System;
using System.Collections.Generic;
using BililiveRecorder.Core;
using BililiveRecorder.Web.Models.Rest.Files;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace BililiveRecorder.Web.Api
{
    [ApiController, Route("api/[controller]", Name = "[controller] [action]")]
    public sealed class FileController : ControllerBase, IDisposable
    {
        private readonly PhysicalFileProvider? fileProvider;
        private bool disposedValue;

        public FileController(IRecorder recorder, BililiveRecorderFileExplorerSettings fileExplorerSettings)
        {
            if (recorder is null) throw new ArgumentNullException(nameof(recorder));
            if (fileExplorerSettings is null) throw new ArgumentNullException(nameof(fileExplorerSettings));
            if (fileExplorerSettings.Enable)
            {
                this.fileProvider = new PhysicalFileProvider(recorder.Config.Global.WorkDirectory!);
            }
        }

        /// <summary>
        /// 获取录播目录文件信息
        /// </summary>
        /// <param name="path" example="/">路径</param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public FileApiResult GetFiles([FromQuery] string path)
        {
            if (this.fileProvider is null || path is null)
                return FileApiResult.NotExist;

            var contents = this.fileProvider.GetDirectoryContents(path);

            if (!contents.Exists)
                return FileApiResult.NotExist;

            var fileLikes = new List<FileLikeDto>();

            foreach (var content in contents)
            {
                try
                {
                    if (!content.Exists)
                        continue;

                    if (content.IsDirectory)
                    {
                        fileLikes.Add(new FolderDto
                        {
                            Name = content.Name,
                            LastModified = content.LastModified,
                        });
                    }
                    else
                    {
                        var pathTrimmed = path.Trim('/');
                        fileLikes.Add(new FileDto
                        {
                            Name = content.Name,
                            LastModified = content.LastModified,
                            Size = content.Length,

                            // Path.Combine 在 Windows 上会用 \
                            Url = "/file/" + (pathTrimmed.Length > 0 ? pathTrimmed + '/' : string.Empty) + content.Name,
                        });
                    }
                }
                catch (Exception) { }
            }

            return new FileApiResult(true, path, fileLikes);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.fileProvider?.Dispose();
                }
                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
