using Microsoft.AspNetCore.Mvc;
using Amazon.S3.Transfer;
using Amazon.S3;
using Amazon.S3.Model;


namespace awsS3Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly IAmazonS3 _s3Client;
        private const string bucketName = " "; //Bucket name here, planned to user env variables later

        public FilesController(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }

        [HttpGet("{Imagen}")]
        public async Task<IActionResult> GetFile(string fileName)
        {
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileName
                };

                using var response = await _s3Client.GetObjectAsync(request);
                using var responseStream = response.ResponseStream;
                using var memoryStream = new MemoryStream();

                await responseStream.CopyToAsync(memoryStream);
                var contentType = response.Headers["Content-Type"];

                return File(memoryStream.ToArray(), contentType, fileName);
            }
            catch (AmazonS3Exception e)
            {
                return NotFound(new { message = e });
            }
        }
        [HttpGet("list")]
        public async Task<IActionResult> ListFiles()
        {
            try
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = bucketName
                };

                var response = await _s3Client.ListObjectsV2Async(request);
                var fileNames = new List<string>();

                foreach (var objects in response.S3Objects)
                {
                    fileNames.Add(objects.Key);
                }

                return Ok(fileNames);
            }
            catch (AmazonS3Exception e)
            {
                return BadRequest(new { message = e });
            }
        }
        [HttpGet("listUrls")]
        public async Task<IActionResult> ListFilesUrls()
        {
            try
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = bucketName
                };

                var response = await _s3Client.ListObjectsV2Async(request);
                var fileUrls = new List<string>();

                foreach (var entry in response.S3Objects)
                {
                    var fileUrl = $"https://{bucketName}.s3.amazonaws.com/{entry.Key}";
                    fileUrls.Add(fileUrl);
                }

                return Ok(fileUrls);
            }
            catch (AmazonS3Exception e)
            {
                return BadRequest(new { message = e });
            }
        }


        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Invalid file." });
            }

            try
            {
                var fileKey = Guid.NewGuid() + Path.GetExtension(file.FileName);

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);

                    var request = new TransferUtilityUploadRequest
                    {
                        InputStream = stream,
                        Key = fileKey,
                        BucketName = bucketName,
                        
                    };

                    var transferUtility = new TransferUtility(_s3Client);
                    await transferUtility.UploadAsync(request);
                }

                var fileUrl = $"https://{bucketName}.s3.amazonaws.com/{fileKey}";

                return Ok(new { url = fileUrl });
            }
            catch (AmazonS3Exception e)
            {
                return BadRequest(new { message = e });
            }
            catch (Exception e)
            {
                return BadRequest(new { message = e });
            }
        }
    }
}