﻿namespace MarkerClustering

open System
open System.Collections.Generic

module Constants =
    let MinLonValue = -180.
    let MaxLonValue = 180.
    let Epsilon = 0.0000000001
    let MergeWithin = 2.9

type MapPoint<'a> = {
    X : float
    Y : float
    Data : 'a
}

type Cluster<'a> = {
    ID : string
    IDX : int
    IDY : int
    X : float
    Y : float
    Count : int
    Points : MapPoint<'a> list
}


module Cluster =

    /// Used by zoom level and deciding the grid size, O(halfSteps)
    /// O(halfSteps) ~  O(maxzoom) ~  O(k) ~  O(1)
    /// Google Maps doubles or halves the view for 1 step zoom level change
    let half(d:float, halfSteps:float) =
        // http://en.wikipedia.org/wiki/Decimal_degrees
        let meter11 = 0.0001 //decimal degrees

        let half = d * Math.Pow(0.5, halfSteps)
        let halfRounded = Math.Round(half, 4)
        // avoid grid span less than this level
        if halfRounded < meter11 then meter11 else halfRounded

    // Dictionary lookup key used by grid cluster algo
    let inline getId(idx:int, idy:int) =
        String.Concat(idx, ";", idy)

    let getPointMappedIds(point:MapPoint<'a>, deltaX, deltaY) =
        let relativeY = point.Y

        let mutable overlapMapMinX = (int)(Constants.MinLonValue / deltaX) - 1
        let mutable overlapMapMaxX = (int)(Constants.MaxLonValue / deltaX)

        // The deltaX = 20 example scenario, then set the value 9 to 8 and -10 to -9

        // Similar to if (LatLonInfo.MaxLonValue % deltax == 0) without floating presicion issue
        if Math.Abs(Constants.MaxLonValue % deltaX - 0.) < Constants.Epsilon then
            overlapMapMaxX <- overlapMapMaxX - 1
            overlapMapMinX <- overlapMapMinX + 1

        let mutable idxx = (int)(point.X / deltaX)
        if point.X < 0. then idxx <- idxx - 1

        if Math.Abs(Constants.MaxLonValue % point.X - 0.) < Constants.Epsilon then
            if point.X < 0. then idxx <- idxx + 1 else idxx <- idxx - 1

        if idxx = overlapMapMinX then idxx <- overlapMapMaxX

        let idx = idxx

        // Latitude never wraps around with Google Maps, ignore 90, -90 wrap-around for latitude
        let idy = (int)(relativeY / deltaY)

        idx, idy

    let getCentroidFromCluster(points:MapPoint<'a> seq) =
        let mutable x = 0.
        let mutable y = 0.
        let mutable count = 0
        for p in points do
            x <- x + p.X
            y <- y + p.Y
            count <- count + 1

        let count = float count
        x / count, y / count

    let getNearestPointFromCentroid(points:MapPoint<'a> seq, (centroidX,centroidY)) =
        let mutable x = centroidX
        let mutable y = centroidY
        let mutable minDist = System.Double.MaxValue
        for p in points do
            let dist =
                let dx = p.X - centroidX
                let dy = p.Y - centroidY
                dx * dx + dy * dy
            if dist < minDist then
                minDist <- dist
                x <- p.X
                y <- p.Y

        x, y

type Clustering<'a>(zoomLevel:float) =
    // Absolute base value of longitude distance, heuristic value
    let xZoomLevel1 = 480.
    // Absolute base value of latitude distance, heuristic value
    let yZoomLevel1 = 240.
    let gridX = 6.
    let gridY = 5.

    // Relative values, used for adjusting grid size
    let deltaX = Cluster.half(xZoomLevel1, zoomLevel - 1.0) / gridX
    let deltaY = Cluster.half(yZoomLevel1, zoomLevel - 1.0) / gridY
    let minDistX = deltaX / Constants.MergeWithin
    let minDistY = deltaY / Constants.MergeWithin

    // If clusters in grid are too close to each other, merge them
    let withinDist = max minDistX minDistY

    with

        member __.MergeClustersGrid(buckets:Dictionary<string,Cluster<'a>>) =
            let keys = buckets.Keys |> Seq.toArray
            for key in keys do
                for x in -1 .. 1 do
                    for y in -1 .. 1 do
                        if x <> 0 || y <> 0 then
                            match buckets.TryGetValue key with
                            | true, current ->
                                let neighborKey = Cluster.getId(current.IDX + x, current.IDY + y)

                                match buckets.TryGetValue neighborKey with
                                | true, neighbor ->
                                    let dx = current.X - neighbor.X
                                    let dy = current.Y - neighbor.Y
                                    let dist = Math.Sqrt(dx * dx + dy * dy)
                                    if dist <= withinDist then
                                        let points = current.Points @ neighbor.Points

                                        // recalc centroid
                                        let x,y = Cluster.getCentroidFromCluster points
                                        buckets.[current.ID] <-
                                            { current
                                                with
                                                    Count = current.Count + neighbor.Count
                                                    Points = points
                                                    X = x
                                                    Y = y }

                                        buckets.Remove neighborKey |> ignore
                                | _ ->
                                    ()
                            | _ ->
                                ()

        member this.RunClustering(points:MapPoint<'a> seq) =
            let buckets = Dictionary<string, Cluster<'a>>()

            // Put points in buckets
            for point in points do
                let idx,idy = Cluster.getPointMappedIds(point, deltaX, deltaY)
                let id = Cluster.getId(idx, idy)

                buckets.[id] <-
                    match buckets.TryGetValue id with
                    | true, bucket ->
                        { bucket with
                            Count = bucket.Count + 1
                            Points = point :: bucket.Points }
                    | _ ->
                        { IDX = idx
                          IDY = idy
                          ID = id
                          X = point.X
                          Y = point.Y
                          Count = 1
                          Points = [point] }


            let clusters = Dictionary<_,_>()
            for bucket in buckets.Values do
                let centroidX,centroidY = Cluster.getCentroidFromCluster bucket.Points
                let x,y = Cluster.getNearestPointFromCentroid(bucket.Points, (centroidX,centroidY))
                clusters.Add(bucket.ID, { bucket with X = x; Y = y })

            this.MergeClustersGrid clusters

            clusters.Values
