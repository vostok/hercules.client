## 0.1.25 (16-12-2024): 

Bump NuGet deps versions

## 0.1.24 (21-11-2024):

Rebuild nuget package to use new version of `Vostok.ClusterClient.Transport`.

## 0.1.21 (17-10-2024):

Realize new method from `IHerculesTagsBuilder` interface. 

## 0.1.19 (13-01-2022):

Optimize GUID creation in .Net 6. Optimize GUID creation in Windows.

## 0.1.18 (06-12-2021):

Added `net6.0` target.

## 0.1.16 (18-10-2021):

Added timeout parameter usage for HerculesManagementClient.

## 0.1.15 (21-09-2021):

Use synchronous sending instead of fire & foget for HerculesSink.

## 0.1.14 (20-07-2021):

Optimize GUID creation on Linux

## 0.1.13 (18-02-2021):

Fix cancellation token race bug on startup.

## 0.1.12 (07-05-2020):

Migrate to K4os.Compression.LZ4 instead of native implementation.

## 0.1.11 (03-04-2020):

Update lz4 library (fixed bin directory location in ASP.NET apps).

## 0.1.10 (07-04-2020):

Update lz4 library (now contains native libraries inside package).

## 0.1.6 (06-04-2020):

Make lz4 library dependency optional.

## 0.1.5 (30-03-2020):

Update lz4 library.

## 0.1.4 (06-03-2020):

Compress traffic with lz4.

## 0.1.3 (03-10-2019):

Implemented garbage collection of unused buffers.

## 0.1.2 (04-09-2019)

Added generic `HerculesTimelineClient<T>` supporting custom event builder.

## 0.1.1 (25-06-2019)

- Added generic `HerculesStreamClient<T>` supporting custom event builder.
- Fixed a slow memory leak caused by incorrect chaining of cancellation tokens and task timeouts.

## 0.1.0 (22-03-2019): 

Initial prerelease.
