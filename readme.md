# MarkerClustering

[![MarkerClusterung on Nuget](https://buildstats.info/nuget/MarkerClustering)](https://www.nuget.org/packages/MarkerClustering/)

This is a netstandard library for clustering map points. It can be used for server-side clustering of marker e.g. for Google Maps and ASP.NET Core.


![Clustering Img](https://raw.githubusercontent.com/pootzko/GoogleMaps.Net.Clustering/master/cluster-map.png "clustering image")

**Original Lib**  

This is highly inspired by [Google Maps Server-side Clustering with C#
](https://github.com/pootzko/GoogleMaps.Net.Clustering), but the algorithm here is a bit simplified.


**Installation**  

* The lastes version of MarkerClusterung is available on [NuGet](https://www.nuget.org/packages/MarkerClustering).


**Usage**

The following code describes a typical usage in ASP.NET Core and GoogleMaps:

        // Retrieve map markers from data store
        let markers = getAllMarkers()

        // filter markers to the bounding box of your map (optional)
        let filtered =
            markers
            |> Array.filter (fun m ->
                m.Lat <= ne.Lat && m.Long >= ne.Long &&
                  m.Lat >= sw.Lat && m.Long <= sw.Long)

        // Map to library's MapPoint<'a>
        let points : MapPoint<_VisibleChargingPoint_> [] =
            filtered
            |> Array.map (fun m -> { X = m.Lat; Y = m.Long; Data = m })

        // run clustering with your map's current zoom level
        let clustering = Clustering(zoomLevel)
        let result = clustering.RunClustering points


**Caching**

If your markers don't change that often then you may want to put the clustering in a separate task.
A good strategy could be to use FaaS Azure Functions or AWS Lambda to calculate all clusters for all zoom levels.
Those clusters could be stored in a distributed cache like Redis, which then allows you to [query the clusters by map center and radius](https://redis.io/commands/georadius).