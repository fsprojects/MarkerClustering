# MarkerClustering

[![MarkerClusterung on Nuget](https://buildstats.info/nuget/MarkerClustering)](https://www.nuget.org/packages/MarkerClustering/)

Library that allows you to group a number of geo-points (points with longitude and latitude coordinates) into clusters based on their relative proximity to each other and the zoom level of the map being used. Since the library targets netstandard, it can be used on the server with .NET Core applications when you want to minimize the number of points returned to the client application and therefore reducing the network traffic by returning clustered points instead. The library can also be used by [Fable](https://github.com/fable-compiler/fable) projects to enable client-side marker clustering when you are using map components which do not have clustering support built-in like with the [Feliz.PigeonMaps](https://zaid-ajaj.github.io/Feliz/#/Ecosystem/PigeonMaps) package where you need to do clustering yourself using this library. Many map components on the client-side will have clustering support by default. You will not need this library when you are using GoogleMaps API because it [already supports](https://cloud.google.com/blog/products/maps-platform/how-cluster-map-markers) clustering.  


![Clustering Img](https://raw.githubusercontent.com/pootzko/GoogleMaps.Net.Clustering/master/cluster-map.png "clustering image")

**Original Lib**  

This library is inspired by [Google Maps Server-side Clustering with C#
](https://github.com/pootzko/GoogleMaps.Net.Clustering), but the algorithm here is a bit simplified for performance reasons.

**Installation**  

* The latest version of MarkerClusterung is available on [NuGet](https://www.nuget.org/packages/MarkerClustering).


**Usage**

The following code describes a typical usage in ASP.NET Core and GoogleMaps:

        // Retrieve map markers from data store
        let markers = getAllMarkers()

        // filter markers to the bounding box of your map (optional)
        let filtered =
            markers
            |> Array.filter (fun m ->
                m.Lat <= ne.Lat && m.Long <= ne.Long &&
                  m.Lat >= sw.Lat && m.Long >= sw.Long)

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
