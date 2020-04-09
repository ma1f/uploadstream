UploadStream - high performance file upload streaming for dotnet
=================================================================
<!--[![pipeline status](https://gitlab.com/ma1f/uploadstream/badges/master/pipeline.svg)](https://gitlab.com/ma1f/uploadstream/commits/master)-->

Installation
------------
Nuget: [https://www.nuget.org/packages/UploadStream](https://www.nuget.org/packages/UploadStream)
```bash
> install-package UploadStream
```

Background
----------
A simplification & rewrite of recommended code for streaming multi-part file uploads as per Microsoft [documentation](https://docs.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-3.1).

Writeup on package and performance results can be read here - [https://medium.com/@ma1f/file-streaming-performance-in-dotnet-4dee608dd953](https://medium.com/@ma1f/file-streaming-performance-in-dotnet-4dee608dd953).

Updated for dotnet core v3.1.

Features
--------
Optimise multi-part streaming file upload performance, offering 10x improvement in performance, and reduced memory allocation (10%-40%).

By default dotnet model form model binding loads the entire stream into memory using `IEnumerable<IFormFile>` - this is non-ideal for large files
where processing of the stream should occur during streaming rather then buffering entire file(s) to memory/disk.

This package allows upload streams to be asynchronously processed via a delegate (`StreamFiles<T>(Action<IFormFile> func)`,
maintaining generic model binding functionality with `ModelState` validation.

Usage
-----
```csharp
[HttpPost("upload")]
public async Task<IActionResult> Upload() {
    // returns a generic typed model, alternatively non-generic overload if no model binding is required
    MyModel model = await this.StreamFiles<MyModel>(async formFile => {
        // implement processing of stream as required via an IFormFile interface
        using (var stream = formfile.OpenReadStream())
            await ...
    });
    // ModelState is still validated from model
    if(!ModelState.IsValid)
        ...
}
```

Performance (Benchmark.Net)
-----------

### Results
Results are normalised in comparison to default dotnet `IFormFile` model binding, the `UploadStream` package offers around 10x performance
improvement and 10%-40% improvement in memory allocation.

Out of interest a comparison to file uploads via a base64 model was performed, interestingly this actually offers improved performance (1.5x - 2.5x)
with a cost of increased memory allocations (0.3x - 0.1x) and increased memory heap trashing (gc gen1/gen2).

| Alias | File sizes | StreamFiles (us/alloc) | Base64 (us/alloc) |
|------ |-----------:|-----------------------:|------------------:|
|    Xs |    5.94 KB |          1.14x / 1.23x |     1.16x / 1.44x |
|    Sm |  106.53 KB |          3.98x / 1.43x |     2.85x / 0.30x |
|    Md |  865.37 KB |          8.66x / 1.24x |     2.36x / 0.09x |
|    Lg |    6.04 MB |          9.86x / 1.09x |     1.97x / 0.11x |
|    Xl |   21.91 MB |          9.16x / 1.08x |     1.72x / 0.14x |


### Upload Performance
Detailed `Benchmark.Net` results for file uploads, comparing `base64`, `IFormFile` and `StreamFiles` for a range of file sizes.
Worth noting that the error and standard deviation range is much tighter for the `StreamFiles` method, meaning much more predictable performance.

|       Method | Filename |         Mean |       Error |      StdDev | CI99.9% Margin |     Gen 0 |     Gen 1 |     Gen 2 |    Allocated |
|------------- |--------- |-------------:|------------:|------------:|---------------:|----------:|----------:|----------:|-------------:|
| UploadBase64 |   xs.png |     363.5 us |    36.83 us |    103.3 us |       36.83 us |         - |         - |         - |     190.4 KB |
| UploadBase64 |   sm.jpg |     708.6 us |    63.66 us |    176.4 us |       63.66 us |         - |         - |         - |   1062.28 KB |
| UploadBase64 |   md.jpg |   3,903.7 us |   125.31 us |    349.3 us |      125.31 us | 1000.0000 | 1000.0000 | 1000.0000 |   8100.71 KB |
| UploadBase64 |   lg.jpg |  27,042.4 us |   746.13 us |  2,104.5 us |      746.13 us | 2000.0000 | 1000.0000 | 1000.0000 |  63570.73 KB |
| UploadBase64 |   xl.exe | 114,938.1 us | 3,249.74 us |  9,219.0 us |    3,249.74 us | 7000.0000 | 4000.0000 | 2000.0000 | 178189.37 KB |
|   UploadFile |   xs.png |     422.0 us |    24.62 us |    69.05 us |       24.62 us |         - |         - |         - |    273.61 KB |
|   UploadFile |   sm.jpg |   2,021.0 us |   130.21 us |   371.50 us |      130.21 us |         - |         - |         - |     313.6 KB |
|   UploadFile |   md.jpg |   9,196.8 us |   516.66 us | 1,498.93 us |      516.66 us |         - |         - |         - |    766.22 KB |
|   UploadFile |   lg.jpg |  53,273.9 us | 1,071.62 us | 2,951.55 us |    1,071.62 us | 1000.0000 |         - |         - |    6821.3 KB |
|   UploadFile |   xl.exe | 197,292.1 us | 4,373.54 us | 7,426.61 us |    4,373.54 us | 4000.0000 | 1000.0000 |         - |  25336.69 KB |
| UploadStream |   xs.png |     371.0 us |    16.29 us |    46.20 us |       16.29 us |         - |         - |         - |     222.6 KB |
| UploadStream |   sm.jpg |     507.4 us |    25.21 us |    71.92 us |       25.21 us |         - |         - |         - |    219.88 KB |
| UploadStream |   md.jpg |   1,061.9 us |    50.26 us |   139.27 us |       50.26 us |         - |         - |         - |    619.09 KB |
| UploadStream |   lg.jpg |   5,404.5 us |   209.60 us |   601.39 us |      209.60 us | 1000.0000 |         - |         - |   6271.68 KB |
| UploadStream |   xl.exe |  21,542.0 us |   429.47 us | 1,232.22 us |      429.47 us | 4000.0000 | 1000.0000 |         - |  23537.77 KB |


#### Key
* UploadBase64 - default json model binding, convert from base64 to byte[]
* UploadFile - default `IFormFile` model binding
* UploadStream - custom `StreamFiles` stream processing and model binding


### Multipart Load Performance - 1x Upload, 20x attached Files
Results of uploading 20x files in one upload with `IEnumerable<IFormFile>` compared to `StreamFiles` show below.
Generally using the optimised `StreamFiles` method offers ~10-15x improvement in performance with slightly less memory allocation required.


### Model binding - [DisableFormModelBinding]
Testing around Model binding appears to show that including this attribute is not required - if the model is defined in the method parameters
then this is bound irrespective of if the attribute is in place or not. If no model is defined in the method parameters (which it shouldn't be
when using `StreamFiles`) then no model binding is attempted.
