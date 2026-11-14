using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Storage;
using Notrelix.Infrastructure.Storage;
using Notrelix.Infrastructure.Storage.Providers;

namespace Notrelix.Infrastructure.Tests.Storage;

/// <summary>
/// Pre-signed URLs of the S3 provider. Pre-signing is local computation with the configured credentials, so these tests make no network
/// call: they check the shape of the URL the AWS SDK presigner produces (host, bucket and key in the path, algorithm, expiry window, the
/// response overrides of a download) and the expiry the provider reports. The object operations need a live bucket and are not covered here.
/// </summary>
public sealed class S3StorageProviderPresignTests
{
    private const string Bucket = "notrelix-private";
    private const string Key = "0123456789abcdef0123456789abcdef/fedcba9876543210fedcba9876543210/attachments/0123456789abcdef0123456789abcdef";

    private static S3StorageProvider Provider(string serviceUrl = "http://minio.test:9000") => new(
        Microsoft.Extensions.Options.Options.Create(new StorageOptions
        {
            Provider = StorageProviderNames.S3,
            S3 = new S3StorageOptions
            {
                Bucket = Bucket,
                ServiceUrl = serviceUrl,
                Region = "us-east-1",
                ForcePathStyle = true,
                AccessKeyId = "AKIATESTONLY",
                SecretAccessKey = "test-secret-not-real",
            },
        }),
        NullLogger<S3StorageProvider>.Instance);

    [Fact]
    public void The_provider_reports_pre_signed_support()
    {
        using var provider = Provider();

        provider.SupportsPresignedUrls.Should().BeTrue();
    }

    [Fact]
    public async Task Upload_url_is_a_put_to_the_bucket_key_path_with_a_five_minute_window()
    {
        using var provider = Provider();
        var before = DateTimeOffset.UtcNow;

        var upload = await provider.CreateUploadUrlAsync(Key, "image/png", 12, TimeSpan.FromMinutes(5), CancellationToken.None);

        upload.Method.Should().Be("PUT");
        upload.ContentType.Should().Be("image/png");
        upload.ContentLength.Should().Be(12);
        var uri = new Uri(upload.Url);
        uri.Host.Should().Be("minio.test");
        uri.AbsolutePath.Should().Be($"/{Bucket}/{Key}", "path-style addressing puts the bucket first");
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        query["X-Amz-Algorithm"].Should().Be("AWS4-HMAC-SHA256");
        query["X-Amz-Expires"].Should().Be("300");
        query["X-Amz-Signature"].Should().NotBeNullOrEmpty();
        upload.ExpiresAt.Should().BeCloseTo(before.AddMinutes(5), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Download_url_is_a_get_that_overrides_the_response_type_and_disposition()
    {
        using var provider = Provider();

        var download = await provider.CreateDownloadUrlAsync(Key, "photo.png", "image/png", inline: true, TimeSpan.FromMinutes(5), CancellationToken.None);

        var query = System.Web.HttpUtility.ParseQueryString(new Uri(download.Url).Query);
        query["X-Amz-Expires"].Should().Be("300");
        query["response-content-type"].Should().Be("image/png");
        query["response-content-disposition"].Should().Contain("inline").And.Contain("photo.png");
        download.Url.Should().Contain(Key);
    }

    [Fact]
    public async Task A_non_inline_download_is_an_attachment_disposition()
    {
        using var provider = Provider();

        var download = await provider.CreateDownloadUrlAsync(Key, "report.pdf", "application/pdf", inline: false, TimeSpan.FromMinutes(5), CancellationToken.None);

        System.Web.HttpUtility.ParseQueryString(new Uri(download.Url).Query)["response-content-disposition"]
            .Should().Contain("attachment").And.Contain("report.pdf");
    }

    [Fact]
    public async Task The_download_window_is_the_configured_one()
    {
        using var provider = Provider();
        var before = DateTimeOffset.UtcNow;

        var download = await provider.CreateDownloadUrlAsync(Key, "photo.png", "image/png", inline: true, TimeSpan.FromSeconds(60), CancellationToken.None);

        System.Web.HttpUtility.ParseQueryString(new Uri(download.Url).Query)["X-Amz-Expires"].Should().Be("60");
        download.ExpiresAt.Should().BeCloseTo(before.AddSeconds(60), TimeSpan.FromSeconds(10));
    }

    [Theory]
    [InlineData("/absolute/key")]
    [InlineData("a/../b")]
    public async Task A_key_that_escapes_the_bucket_layout_is_refused(string key)
    {
        using var provider = Provider();

        var act = () => provider.CreateUploadUrlAsync(key, "image/png", 12, TimeSpan.FromMinutes(5), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public void Storage_options_for_s3_require_a_bucket_and_a_paired_credential()
    {
        var validator = new StorageOptionsValidator();

        validator.Validate(null, new StorageOptions { Provider = StorageProviderNames.S3 }).Succeeded.Should().BeFalse();
        validator.Validate(null, new StorageOptions
        {
            Provider = StorageProviderNames.S3,
            S3 = new S3StorageOptions { Bucket = "b", AccessKeyId = "only-the-id" },
        }).Succeeded.Should().BeFalse();
        validator.Validate(null, new StorageOptions
        {
            Provider = StorageProviderNames.S3,
            S3 = new S3StorageOptions { Bucket = "b", ServiceUrl = "ftp://wrong" },
        }).Succeeded.Should().BeFalse();
        validator.Validate(null, new StorageOptions
        {
            Provider = StorageProviderNames.S3,
            S3 = new S3StorageOptions { Bucket = "b", ServiceUrl = "http://minio.test:9000", AccessKeyId = "id", SecretAccessKey = "s" },
        }).Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Storage_options_default_to_the_filesystem_provider()
    {
        new StorageOptionsValidator().Validate(null, new StorageOptions()).Succeeded.Should().BeTrue();
        new StorageOptions().Provider.Should().Be(StorageProviderNames.FileSystem);
    }

    [Theory]
    [InlineData(0, 300)]
    [InlineData(300, 0)]
    [InlineData(3601, 300)]
    public void Presigned_url_lifetimes_are_bounded_to_one_hour(int upload, int download)
    {
        var options = new PresignedUrlOptions { UploadExpirySeconds = upload, DownloadExpirySeconds = download };

        new PresignedUrlOptionsValidator().Validate(null, options).Succeeded.Should().BeFalse();
    }

    [Fact]
    public void The_filesystem_provider_reports_no_pre_signed_support()
    {
        var presign = new NoPresignedObjectStorage();

        presign.SupportsPresignedUrls.Should().BeFalse();
    }
}
