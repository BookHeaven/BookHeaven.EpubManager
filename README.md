# BookHeaven EbookManager
BookHeaven EbookManager is a .NET library developed for the BookHeaven ecosystem, but it can be used independently in other projects.<br/>
It exposes a set of Services to manipulate eBooks in a limited amount of formats.<br/>

More detailed documentation will be added in the future.

## :rocket: How to use
1. Register the services in your <code>program.cs</code>
```csharp
builder.Services.AddEbookManager();
```
2. Inject the EbookManagerProvider service and use it to get the appropriate reader or writer for your desired format at runtime.
```csharp
public class MyService(EbookManagerProvider ebookManagerProvider)
{
    private void MyMethod() 
    {
        var reader = ebookManagerProvider.GetReader(Format.Epub);
        var writer = ebookManagerProvider.GetWriter(Format.Epub);
    }
}
```

## :book: Supported Formats and features

<table>
    <thead>
        <tr>
            <th>Format</th>
            <th>Read Metadata</th>
            <th>Replace Metadata</th>
            <th>Extract Cover</th>
            <th>Replace Cover</th>
            <th>Read Contents</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td>Epub</td>
            <td>✔️</td>
            <td>✔️</td>
            <td>✔️</td>
            <td>✔️</td>
            <td>✔️</td>
        </tr>
        <tr>
            <td>Pdf</td>
            <td>✔️</td>
            <td>✔️[^1]</td>
            <td>✔️[^2]</td>
            <td>✔️</td>
            <td>✔️[^3]</td>
        </tr>
    </tbody>
</table>

[^1]: Metadata in PDF files is limited to title and author, and most of the time they are empty.
[^2]: The cover is extracted from the first image of the first page of the PDF, so it will fail if there isn't any images there.
[^3]: Text extraction is very basic and may not work properly with complex layouts. It works best with image based PDFs like comics and mangas.

## :package: Credits
- HtmlAgilityPack (https://html-agility-pack.net/)
- HtmlAgilityPack.CssSelectors.NetCore (https://github.com/trenoncourt/HtmlAgilityPack.CssSelectors.NetCore)
- iText Core (https://github.com/itext/itext-dotnet)
- SkiaSharp (https://github.com/mono/SkiaSharp)