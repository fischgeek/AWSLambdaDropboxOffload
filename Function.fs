namespace AWSLambdaDropboxOffload

open Amazon.Lambda.Core

open System
open Dropbox.Api
open Dropbox.Api.Files

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[<assembly: LambdaSerializer(typeof<Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer>)>]
()

type Function() =
    member __.FunctionHandler (input: string) (_: ILambdaContext) =
        let dbx = new DropboxClient(input)

        let offloadDir = "/Archive"
        let targetDir = "/Archive/Archived"
        
        (dbx.Files.CreateFolderV2Async offloadDir) |> ignore
        (dbx.Files.CreateFolderV2Async targetDir) |> ignore

        (dbx.Files.ListFolderAsync(offloadDir, false)
            |> Async.AwaitTask
            |> Async.RunSynchronously
        ).Entries
        |> Seq.toList
        |> Seq.map (fun f -> RelocationPath(f.PathLower, $"{targetDir}/{f.Name}"))
        |> Seq.chunkBySize 500
        |> Seq.iter (fun chunk -> dbx.Files.MoveBatchV2Async(chunk, true) |> ignore)
        Environment.Exit