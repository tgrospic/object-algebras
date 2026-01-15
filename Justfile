solution := "ObjectAlgebras.sln"

restore:
    dotnet restore {{solution}}

build:
    dotnet build {{solution}} -c Release --no-restore

fmt:
    dotnet format {{solution}}

pack:
    dotnet pack {{solution}} -c Release

run-parser:
    dotnet run --project Samples/ParserSample/ParserSample.csproj

run-banking:
    dotnet run --project Samples/BankingAppSample/Banking.Main/Banking.Main.csproj

clean:
    dotnet clean {{solution}} -c Release
