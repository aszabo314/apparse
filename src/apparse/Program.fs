open System
open System.IO
open Aardvark.Base
open FSharp.Data.Adaptive
open Aardvark.Data.E57
open Aardvark.Data.Points
open Aardvark.Geometry.Points


[<EntryPoint;STAThread>]
let main argv =
    Aardvark.Init()
    
    // las/laz
    // comprehensive las parsing here: https://github.com/aardvark-platform/aardvark.algodat/blob/65fd61fc3ab40d211eb23fb33bec88c57ca32fe1/src/Aardvark.Data.Points.LasZip/Parser.cs#L165
    use lasStream = File.OpenRead(@"C:\bla\pts\TEST DATEI_resampled.laz")
    let points = LASZip.Parser.ReadPoints(lasStream, 8192)
    points |> Seq.iter (fun pts -> printfn "%A points found" pts.Count)

    // ply
    // comprehensive ply parsing here: https://github.com/aardvark-platform/aardvark.algodat/blob/65fd61fc3ab40d211eb23fb33bec88c57ca32fe1/src/Aardvark.Data.Points.Ply/PlyImport.cs#L68
    use plyStream = File.OpenRead(@"C:\bla\pts\UAV_JBs-Haus.austriaGKeast.ply")
    let plyDataset = Ply.Net.PlyParser.Parse(plyStream, 8192, printfn "%A")
    let chunks = Aardvark.Data.Points.Import.Ply.Chunks(plyDataset)
    chunks |> Seq.iter (fun chunk -> printfn "%A points found" chunk.Count)

    // e57
    // comprehensive e57 parsing her: https://github.com/aardvark-platform/aardvark.algodat/blob/65fd61fc3ab40d211eb23fb33bec88c57ca32fe1/src/Aardvark.Data.E57/ImportE57.cs#L169
    use e57Stream = File.OpenRead(@"C:\bla\pts\lowergetikum 20230321.e57")
    let header = ASTM_E57.E57FileHeader.Parse(e57Stream, e57Stream.Length, true)
    header.E57Root.Data3D |> Seq.iter (fun d3d -> 
        // access raw data like this
        let pts = d3d.StreamPointsFull(8192, true, System.Collections.Immutable.ImmutableHashSet.Empty)
        printfn "%A points found" (pts |> Seq.sumBy (fun struct(pts,_) -> pts.Length))
    )

    // All file parsers supply point data using "Chunks". We can write our own:
    let positions = 
        [|
            for x in 0.0 .. 0.1 .. 10.0 do 
                for y in 0.0 .. 0.1 .. 10.0 do 
                    for z in 0.0 .. 0.1 .. 10.0 do 
                        yield V3d(x,y,z)
        |]
    let classifications = Array.replicate positions.Length 42uy
    let chunks = [ Aardvark.Data.Points.Chunk(positions=positions, classifications=classifications) ] 
    
    // Chunks are recorded into a "Point Cloud Store".
    let cfg = 
        ImportConfig.Default
                // On disk store ...
            //.WithStorage(PointCloud.OpenStore(@"C:\temp\teststore", LruDictionary(1L <<< 30)))
                // ... or in memory store
            .WithInMemoryStore()
            .WithKey("points")

    let pointCloud = Aardvark.Geometry.Points.PointCloud.Chunks(chunks,cfg)

    // Example operations on a point cloud
    let filtered = FilteredNode.Create(pointCloud.Root.Value, FilterClassification(42uy))
    let queried = Queries.QueryPointsInsideBox(filtered, Box3d.Unit)
    printfn "queried %A chunks" (queried |> Seq.length)

    0