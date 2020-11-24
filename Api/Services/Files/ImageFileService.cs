using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using CSharpFunctionalExtensions;
using HappyTravel.AmazonS3Client.Services;
using HappyTravel.Edo.Api.Infrastructure;
using HappyTravel.Edo.Api.Infrastructure.Options;
using HappyTravel.Edo.Api.Models.Agencies;
using HappyTravel.Edo.Api.Models.Agents;
using Imageflow.Fluent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using HappyTravel.Edo.Data;
using HappyTravel.Edo.Data.Agents;
using Microsoft.EntityFrameworkCore;

namespace HappyTravel.Edo.Api.Services.Files
{
    public class ImageFileService : IImageFileService
    {
        public ImageFileService(IAmazonS3ClientService amazonS3ClientService,
            IOptions<ImageFileServiceOptions> options,
            EdoContext edoContext,
            IDateTimeProvider dateTimeProvider)
        {
            _amazonS3ClientService = amazonS3ClientService;
            _bucketName = options.Value.Bucket;
            _s3FolderName = options.Value.S3FolderName;
            _edoContext = edoContext;
            _dateTimeProvider = dateTimeProvider;
        }


        public Task<Result> SetBanner(IFormFile file, AgentContext agentContext) => AddOrReplace(file, BannerImageName, agentContext);


        public Task<Result> SetLogo(IFormFile file, AgentContext agentContext) => AddOrReplace(file, LogoImageName, agentContext);


        public Task<Result> DeleteBanner(AgentContext agentContext) => Delete(BannerImageName, agentContext);


        public Task<Result> DeleteLogo(AgentContext agentContext) => Delete(LogoImageName, agentContext);


        public Task<Result<SlimUploadedImage>> GetBanner(AgentContext agentContext) => GetImage(BannerImageName, agentContext);


        public Task<Result<SlimUploadedImage>> GetLogo(AgentContext agentContext) => GetImage(LogoImageName, agentContext);


        private async Task<Result> AddOrReplace(IFormFile file, string fileName, AgentContext agentContext)
        {
            return await Validate()
                .Bind(Upload)
                .Map(StoreLink);


            async Task<Result<byte[]>> Validate()
            {
                return await Result.Success()
                    .Ensure(() => file != default, "Could not get the file")
                    .Ensure(() => ImageExtensions.Contains(Path.GetExtension(file?.FileName)?.ToLowerInvariant()),
                        "The file must have extension of a jpeg image")
                    .Map(GetImageBytes)
                    .Bind(ValidateImage);


                async Task<byte[]> GetImageBytes()
                {
                    await using var stream = file.OpenReadStream();
                    using var binaryReader = new BinaryReader(stream);
                    return binaryReader.ReadBytes((int)file.Length);
                }


                async Task<Result<byte[]>> ValidateImage(byte[] imageBytes)
                {
                    // Here should be checks like aspect ratio, for now only resolution checks
                    var imageInfo = await ImageJob.GetImageInfo(new BytesSource(imageBytes));

                    return Result.Success()
                        .Ensure(() => imageInfo.ImageWidth >= MinimumWidth && imageInfo.ImageWidth <= MaximumWidth,
                            $"Image width must be in range from {MinimumWidth} to {MaximumWidth}")
                        .Ensure(() => imageInfo.ImageHeight >= MinimumHeight && imageInfo.ImageHeight <= MaximumHeight,
                            $"Image height must be in range from {MinimumWidth} to {MaximumWidth}")
                        .Map(() => imageBytes);
                }
            }


            async Task<Result<string>> Upload(byte[] imageBytes)
            {
                await using var stream = new MemoryStream(imageBytes);
                return await _amazonS3ClientService.Add(_bucketName, GetKey(agentContext.AgencyId, fileName), stream, S3CannedACL.PublicRead);
            }


            async Task StoreLink(string url)
            {
                var oldUploadedImage = await _edoContext.UploadedImages
                    .SingleOrDefaultAsync(i => i.AgencyId == agentContext.AgentId && i.FileName == fileName);

                if (oldUploadedImage == null)
                {
                    var now = _dateTimeProvider.UtcNow();

                    var newUploadedImage = new UploadedImage
                    {
                        AgencyId = agentContext.AgencyId,
                        Url = url,
                        FileName = fileName,
                        Created = now,
                        Updated = now
                    };

                    _edoContext.Add(newUploadedImage);
                }
                else
                {
                    oldUploadedImage.Updated = _dateTimeProvider.UtcNow();
                    oldUploadedImage.Url = url;

                    _edoContext.Update(oldUploadedImage);
                }

                await _edoContext.SaveChangesAsync();
            }
        }


        private async Task<Result> Delete(string fileName, AgentContext agentContext)
        {
            return await GetImageRecord()
                .Bind(DeleteFromS3)
                .Tap(DeleteRecord);


            async Task<Result<UploadedImage>> GetImageRecord()
            {
                var image = await _edoContext.UploadedImages
                    .SingleOrDefaultAsync(i => i.AgencyId == agentContext.AgentId && i.FileName == fileName);

                return image == null
                    ? Result.Failure<UploadedImage>("Could not find image")
                    : Result.Success(image);
            }


            async Task<Result<UploadedImage>> DeleteFromS3(UploadedImage image)
            {
                var (isSuccess, _, _) = await _amazonS3ClientService.Delete(_bucketName, GetKey(agentContext.AgencyId, fileName));

                return isSuccess
                    ? Result.Success(image)
                    : Result.Failure<UploadedImage>("Error during deleting image from storage");
            }


            async Task DeleteRecord(UploadedImage image)
            {
                _edoContext.UploadedImages.Remove(image);
                await _edoContext.SaveChangesAsync();
            }
        }


        private async Task<Result<SlimUploadedImage>> GetImage(string fileName, AgentContext agentContext)
        {
            var image = await _edoContext.UploadedImages
                .Where(i => i.AgencyId == agentContext.AgencyId && i.FileName == fileName)
                .SingleOrDefaultAsync();

            return image == null
                ? Result.Failure<SlimUploadedImage>("Could not find image")
                : new SlimUploadedImage(image.FileName, image.Url, image.Created, image.Updated);
        }


        private string GetKey(int agencyId, string fileName) => $"{_s3FolderName}/{agencyId}/{fileName.ToLowerInvariant()}";


        private const int MinimumWidth = 200;
        private const int MaximumWidth = 800;
        private const int MinimumHeight = 200;
        private const int MaximumHeight = 800;

        private const string BannerImageName = "banner";
        private const string LogoImageName = "logo";

        private static readonly HashSet<string> ImageExtensions = new HashSet<string>{".jpeg", ".jpg"};

        private readonly IAmazonS3ClientService _amazonS3ClientService;
        private readonly string _bucketName;
        private readonly string _s3FolderName;
        private readonly EdoContext _edoContext;
        private readonly IDateTimeProvider _dateTimeProvider;
    }
}
