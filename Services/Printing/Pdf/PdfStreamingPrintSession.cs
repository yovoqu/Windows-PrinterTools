using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Channels;
using System.Drawing;
using System.Drawing.Printing;
using Windows.Data.Pdf;
using WindowsPrinter.Infrastructure;

namespace WindowsPrinter.Services.Printing.Pdf;

/// <summary>
/// Streams PDF pages through a capacity-1 channel: WinRT renders on the caller apartment,
/// GDI+ consumes one page at a time in a single print job (O(1) page memory).
/// </summary>
internal static class PdfStreamingPrintSession
{
    private sealed class RenderedPage(Image image, bool hasMorePages) : IDisposable
    {
        public Image Image { get; } = image;
        public bool HasMorePages { get; } = hasMorePages;
        public void Dispose() => Image.Dispose();
    }

    public static async Task PrintAsync(
        PdfDocument pdfDocument,
        PrintRequest request,
        CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<RenderedPage>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true
        });

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var renderTask = RenderPagesAsync(pdfDocument, channel.Writer, linkedCts.Token);

        try
        {
            await Task.Run(() => PrintFromChannel(channel.Reader, request, linkedCts.Token), linkedCts.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            linkedCts.Cancel();
            throw;
        }
        finally
        {
            await renderTask.ConfigureAwait(true);
        }
    }

    private static async Task RenderPagesAsync(
        PdfDocument pdfDocument,
        ChannelWriter<RenderedPage> writer,
        CancellationToken cancellationToken)
    {
        try
        {
            for (uint pageIndex = 0; pageIndex < pdfDocument.PageCount; pageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Image? image = null;
                try
                {
                    image = await PdfPageRenderer.RenderPageToImageAsync(pdfDocument, pageIndex).ConfigureAwait(true);
                    var hasMore = pageIndex + 1 < pdfDocument.PageCount;
                    await writer.WriteAsync(new RenderedPage(image, hasMore), cancellationToken).ConfigureAwait(true);
                    image = null;
                }
                catch
                {
                    image?.Dispose();
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            writer.Complete(ex);
            throw;
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private static void PrintFromChannel(
        ChannelReader<RenderedPage> reader,
        PrintRequest request,
        CancellationToken cancellationToken)
    {
        Exception? printError = null;

        using var printDocument = GdiPrintHelper.CreatePrintDocument(request.PrinterName);
        printDocument.PrintPage += (_, e) =>
        {
            if (printError is not null)
            {
                e.Cancel = true;
                return;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!TryReadNextPage(reader, cancellationToken, out var page) || page is null)
                {
                    e.HasMorePages = false;
                    return;
                }

                using (page)
                {
                    GdiPrintHelper.DrawScaledImage(e.Graphics!, page.Image, e.MarginBounds, request.UseColor);
                    e.HasMorePages = page.HasMorePages;
                }
            }
            catch (Exception ex)
            {
                printError = ex;
                e.Cancel = true;
            }
        };

        printDocument.Print();

        if (printError is not null)
            throw printError;

        PropagateChannelFailure(reader);
    }

    private static bool TryReadNextPage(
        ChannelReader<RenderedPage> reader,
        CancellationToken cancellationToken,
        out RenderedPage? page)
    {
        page = null;
        if (reader.TryRead(out var immediate))
        {
            page = immediate;
            return true;
        }

        if (!reader.WaitToReadAsync(cancellationToken).AsTask().GetAwaiter().GetResult())
        {
            PropagateChannelFailure(reader);
            return false;
        }

        if (reader.TryRead(out page))
            return true;

        PropagateChannelFailure(reader);
        return false;
    }

    private static void PropagateChannelFailure(ChannelReader<RenderedPage> reader)
    {
        if (reader.Completion.IsFaulted)
            reader.Completion.GetAwaiter().GetResult();
    }
}
