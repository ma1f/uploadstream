UploadStream - high performance file upload streaming for dotnet
=================================================================
[![pipeline status](https://gitlab.com/ma1f/uploadstream/badges/master/pipeline.svg)](https://gitlab.com/ma1f/uploadstream/commits/master)

Installation
------------
Nuget: [https://www.nuget.org/packages/UploadStream](https://www.nuget.org/packages/UploadStream)
```bash
> install-package UploadStream
```

Features
--------
Optimise multi-part streaming file upload performance, reduced CPU usage ~25% (us), reduced Memory impact ~50% (gen0 gc).

By default dotnet model form model binding loads the entire stream into memory using `IEnumerable<IFormFile>` - this is non-ideal for large files
where processing of the stream should occur while streaming rather then buffering entire file(s) to memory/disk.

This package allows upload streams to be asynchronously processed via a delegate (`StreamFiles<T>(Action<IFormFile> func)`,
maintaining generic model binding functionality with `ModelState` validation - default form model binding is disabled via a
custom `[DisableModelBinding]` attribute.

Usage
-----
```csharp
[HttpPost("upload")]
// disable default model binding in order to enable processing of streams without buffering entire stream (note that this is only needed if Model binding via route/querystring etc is required, if no arguments are passed in then no FormModel binding is triggered).
[DisableModelBinding]
public async Task<IActionResult> Upload(MyRouteModel routeModel) {
    // returns a generic typed model, optionally a non-generic overload is offered if no model binding is required
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
Results are normalised in comparison to default dotnet `IFormFile` model binding, the `UploadStream` package offers performance improvements
which converges at ~25% reduced CPU usage (us), and ~50% reduced memory impact (gen 0 garbage collection) for typical photo sizes and
larger (6MB+), performance improvements for smaller files (10kb-1Mb) range from 5%-20% reduced CPU usage and 5%-30% reduced memory impact.

Out of interest a comparison to file uploads via a base64 json model was performed, interestingly this actually offers improved performance (~15%)
for very small file sizes (<10kb), however becames extremely unwieldy, particularly at anything over the streaming buffer size (64kb or 80kb) -
a huge ~10-20x slower and high memory heap thrashing. The base64 model typically results in 30-40% increased bandwidth usage in addition to
the increased CPU and Memory usage.

| Alias | File sizes | StreamFiles (us/gc) | Base64 (us) |
|------ |-----------:|--------------------:|------------:|
|    Xs |    5.94 KB |       0.95x / 0.98x |       0.86x |
|    Sm |  106.53 KB |       0.94x / 0.95x |       4.11x |
|    Md |  865.37 KB |       0.87x / 0.72x |      10.25x |
|    Lg |    6.04 MB |       0.78x / 0.55x |      15.02x |
|    Xl |   21.91 MB |       0.73x / 0.52x |      16.28x |
|   Xxl |  124.26 MB |       0.77x / 0.50x |      18.90x |


### Upload Performance
Detailed `Benchmark.Net` results for file uploads, comparing `base64`, `IFormFile` and `StreamFiles` for a range of file sizes.
Worth noting that the error and standard deviation range is much tighter for the `StreamFiles` method, meaning much more predictable performance.

|       Method | Filename |           Mean |         Error |        StdDev |     Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|------------- |--------- |---------------:|--------------:|--------------:|----------:|----------:|----------:|----------:|
| UploadBase64 |   xs.png |       169.9 us |      2.011 us |      1.882 us |   12.9395 |         - |         - |   5.84 KB |
| UploadBase64 |   sm.jpg |       979.0 us |     19.165 us |     28.686 us |   77.1484 |   71.2891 |   71.2891 |      6 KB |
| UploadBase64 |   md.jpg |     6,566.9 us |     62.847 us |     58.787 us |  257.8125 |  257.8125 |  257.8125 |   5.84 KB |
| UploadBase64 |   lg.jpg |    52,662.0 us |  1,263.115 us |  3,724.325 us |  687.5000 |  687.5000 |  687.5000 |   5.85 KB |
| UploadBase64 |   xl.exe |   202,926.4 us |  2,817.727 us |  2,635.704 us |  687.5000 |  687.5000 |  687.5000 |   5.86 KB |
| UploadBase64 |  xxl.exe | 1,259,994.9 us | 22,119.049 us | 20,690.170 us | 1062.5000 | 1062.5000 | 1062.5000 |   5.86 KB |
|   UploadFile |   xs.png |       197.9 us |      2.892 us |     2.7051 us |   16.1133 |         - |         - |   5.91 KB |
|   UploadFile |   sm.jpg |       238.2 us |      1.125 us |     0.8786 us |   18.3105 |         - |         - |   5.91 KB |
|   UploadFile |   md.jpg |       640.4 us |     12.682 us |    32.5092 us |   35.1563 |         - |         - |   5.91 KB |
|   UploadFile |   lg.jpg |     3,505.3 us |     63.092 us |    59.0159 us |  156.2500 |         - |         - |   5.93 KB |
|   UploadFile |   xl.exe |    12,468.4 us |    248.525 us |   496.3313 us |  515.6250 |         - |         - |   5.98 KB |
|   UploadFile |  xxl.exe |    66,658.4 us |  1,276.052 us | 1,193.6200 us | 2875.0000 |         - |         - |   6.21 KB |
| UploadStream |   xs.png |       188.7 us |      3.755 us |      3.513 us |   15.8691 |         - |         - |    6.5 KB |
| UploadStream |   sm.jpg |       224.5 us |      2.456 us |      2.177 us |   17.3340 |         - |         - |   6.51 KB |
| UploadStream |   md.jpg |       555.5 us |     11.037 us |     21.000 us |   25.3906 |         - |         - |   6.51 KB |
| UploadStream |   lg.jpg |     2,739.8 us |     34.512 us |     32.283 us |   85.9375 |         - |         - |   6.53 KB |
| UploadStream |   xl.exe |     9,048.8 us |     50.593 us |     42.247 us |  265.6250 |         - |         - |   6.64 KB |
| UploadStream |  xxl.exe |    51,130.5 us |    410.202 us |    383.703 us | 1437.5000 |         - |         - |   6.88 KB |

#### Key
* UploadBase64 - default json model binding, convert from base64 to byte[]
* UploadFile - default `IFormFile` model binding
* UploadStream - custom `StreamFiles` stream processing and model binding


### Load Performance
Compares `base64`, `IFormFile` and `StreamFiles` for a range of file sizes, 20x file upload/API calls - uploaded in parallel.
Results normalised to `IFormFile` (UploadFile) as a default baseline.

| Alias | File sizes | StreamFiles (us/gc) | Base64 (us) | 
|------ |-----------:|--------------------:|------------:|
|    Xs |     5.94kB |       0.95x / 1.05x |       0.88x |
|    Sm |   106.53kB |       1.03x / 0.94x |       3.28x |
|    Md |   865.37kB |       0.87x / 0.66x |      17.53x |
|    Lg |     6.04MB |       0.71x / 0.58x |      37.89x |
|    Xl |    21.91MB |       0.67x / 0.45x |      39.52x |

|       Method | Filename |         Mean |      Error |     StdDev |     Gen 0 |     Gen 1 |     Gen 2 | Allocated |
|------------- |--------- |-------------:|-----------:|-----------:|----------:|----------:|----------:|----------:|
| UploadBase64 |   xs.png |     1.076 ms |  0.0211 ms |  0.0421 ms |  250.0000 |   62.5000 |         - |  17.95 KB |
| UploadBase64 |   sm.jpg |     4.457 ms |  0.1062 ms |  0.1654 ms |   62.5000 |   62.5000 |         - |  13.32 KB |
| UploadBase64 |   md.jpg |    42.683 ms |  0.8300 ms |  1.1081 ms | 1187.5000 | 1125.0000 | 1062.5000 |  22.48 KB |
| UploadBase64 |   lg.jpg |   421.481 ms |  8.3976 ms |  8.9854 ms | 2937.5000 | 2812.5000 | 2812.5000 |  15.18 KB |
| UploadBase64 |   xl.exe | 1,380.365 ms | 27.2765 ms | 28.0110 ms | 1937.5000 | 1875.0000 | 1812.5000 |   7.91 KB |
|   UploadFile |   xs.png |     1.221 ms |  0.0241 ms |  0.0313 ms |  298.8281 |   70.3125 |         - |  11.63 KB |
|   UploadFile |   sm.jpg |     1.360 ms |  0.0237 ms |  0.0282 ms |  332.0313 |   70.3125 |         - |  13.32 KB |
|   UploadFile |   md.jpg |     2.435 ms |  0.0861 ms |  0.2540 ms |  468.7500 |         - |         - |  16.71 KB |
|   UploadFile |   lg.jpg |    11.125 ms |  0.2914 ms |  0.8409 ms | 1500.0000 |  125.0000 |         - |  13.41 KB |
|   UploadFile |   xl.exe |    34.925 ms |  0.9935 ms |  2.8346 ms | 4812.5000 |  187.5000 |         - |  13.79 KB |
| UploadStream |   xs.png |     1.165 ms |  0.0232 ms |  0.0510 ms |  312.5000 |         - |         - |  16.35 KB |
| UploadStream |   sm.jpg |     1.406 ms |  0.0298 ms |  0.0856 ms |  312.5000 |   31.2500 |         - |  17.16 KB |
| UploadStream |   md.jpg |     2.127 ms |  0.0727 ms |  0.2062 ms |  312.5000 |   62.5000 |         - |  10.45 KB |
| UploadStream |   lg.jpg |     7.904 ms |  0.2212 ms |  0.6381 ms |  875.0000 |   62.5000 |         - |  15.13 KB |
| UploadStream |   xl.exe |    23.308 ms |  0.7664 ms |  2.1866 ms | 2187.5000 |   93.7500 |         - |   9.99 KB |

#### Key
* UploadBase64 - default json model binding, convert from base64 to byte[], 20x parallel uploads, default json model binding, convert from base64 to byte[]
* UploadFile - default `IFormFile` model binding, 20x parallel uploads, default `IFormFile` model binding
* UploadStream - custom `StreamFiles` stream processing and model binding, 20x parallel uploads, custom `StreamFiles` stream processing and model binding

### Multipart Load Performance - 1x Upload, 20x attached Files
Results of uploading 20x files in one upload with `IEnumerable<IFormFile>` compared to `StreamFiles` show below.
Interestingly for smaller file sizes `IEnumerable<IFormFile>` demonstrates better performance/memory, however
after ~1Mb in size `StreamFiles` quickly becmoes more highly performant.

| Alias | File sizes | StreamFiles (us/gc) |
|------ |-----------:|----------------------:|
|    Xs |     5.94kB |         1.16x / 1.23x |
|    Sm |   106.53kB |         1.12x / 1.20x |
|    Md |   865.37kB |         0.96x / 1.01x |
|    Lg |     6.04MB |         0.75x / 0.69x |
|    Xl |    21.91MB |         0.76x / 0.39x |

|       Method | Filename |        Mean |      Error |    StdDev |    Gen 0 | Allocated |
|------------- |--------- |------------:|-----------:|----------:|---------:|----------:|
|   UploadFile |   xs.png |    497.9 us |   9.949 us |  19.17 us |  46.8750 |   5.93 KB |
|   UploadFile |   sm.jpg |    553.9 us |  10.999 us |  17.76 us |  48.8281 |   5.93 KB |
|   UploadFile |   md.jpg |    971.5 us |  19.394 us |  51.77 us |  66.4063 |   5.96 KB |
|   UploadFile |   lg.jpg |  4,307.9 us |  85.896 us | 171.54 us | 187.5000 |   6.11 KB |
|   UploadFile |   xl.exe | 12,956.3 us | 255.218 us | 404.80 us | 546.8750 |   6.33 KB |
| UploadStream |   xs.png |    579.3 us |   11.41 us |  17.76 us |  57.6172 |  17.93 KB |
| UploadStream |   sm.jpg |    619.6 us |   12.37 us |  29.40 us |  58.5938 |  18.02 KB |
| UploadStream |   md.jpg |    929.2 us |   17.55 us |  18.78 us |  67.3828 |  18.02 KB |
| UploadStream |   lg.jpg |  3,225.7 us |   60.71 us |  56.79 us | 128.9063 |  18.09 KB |
| UploadStream |   xl.exe |  9,784.6 us |  188.82 us | 252.07 us | 312.5000 |  19.58 KB |

#### Key
* UploadFile - 1x upload, 20x files, default `IEnumerable<FormFile>` model binding
* UploadStream - 1x upload, 20x files, custom `StreamFiles` stream processing and model binding

### Model binding - [DisableFormModelBinding]
Testing around Model binding confirms that if no model is specified as parameters for the controller method, then using
the [DisableFormModelBinding] attibute is unnecessary, model binding and subsequent buffering of the entire stream is
only triggered if a form model is specified.

|                         Method | Filename |        Mean |      Error |     StdDev |    Gen 0 | Allocated |
|------------------------------- |--------- |------------:|-----------:|-----------:|---------:|----------:|
|            UploadStreamNoModel |   xs.png |    176.2 us |   3.040 us |   2.844 us |  15.1367 |   6.52 KB |
|            UploadStreamNoModel |   sm.jpg |    211.2 us |   2.242 us |   1.987 us |  16.3574 |   6.52 KB |
|            UploadStreamNoModel |   md.jpg |    535.4 us |  10.671 us |  23.868 us |  25.3906 |   6.53 KB |
|            UploadStreamNoModel |   lg.jpg |  2,710.1 us |  42.886 us |  40.115 us |  85.9375 |   6.54 KB |
|            UploadStreamNoModel |   xl.exe |  9,154.8 us |  62.123 us |  58.110 us | 265.6250 |   6.66 KB |
| UploadStreamNoModelWithBinding |   xs.png |    187.5 us |   3.059 us |   2.861 us |  15.8691 |   6.59 KB |
| UploadStreamNoModelWithBinding |   sm.jpg |    233.6 us |   3.699 us |   3.460 us |  17.3340 |    6.6 KB |
| UploadStreamNoModelWithBinding |   md.jpg |    560.1 us |  11.201 us |  22.370 us |  25.3906 |   6.61 KB |
| UploadStreamNoModelWithBinding |   lg.jpg |  2,711.6 us |  42.685 us |  39.927 us |  85.9375 |   6.62 KB |
| UploadStreamNoModelWithBinding |   xl.exe |  9,396.8 us | 183.534 us | 232.111 us | 265.6250 |   6.74 KB |
|   UploadStreamModelWithBinding |   xs.png |    276.7 us |   3.505 us |   3.279 us |  26.3672 |   7.39 KB |
|   UploadStreamModelWithBinding |   sm.jpg |    376.6 us |   7.402 us |  13.156 us |  28.8086 |   7.41 KB |
|   UploadStreamModelWithBinding |   md.jpg |  1,015.5 us |  19.967 us |  28.636 us |  44.9219 |   7.42 KB |
|   UploadStreamModelWithBinding |   lg.jpg |  5,261.9 us |  66.016 us |  61.752 us | 164.0625 |   7.44 KB |
|   UploadStreamModelWithBinding |   xl.exe | 18,390.7 us | 366.322 us | 421.857 us | 531.2500 |   7.67 KB |
