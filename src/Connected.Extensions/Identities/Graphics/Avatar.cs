using SkiaSharp;

namespace Connected.Identities.Graphics;
internal static class Avatar
{
	private static readonly List<SKColor> Colors = [];
	private static readonly Random _r = new();

	static Avatar()
	{
		Colors.AddRange([
			new SKColor(33,150,243),
			new SKColor(255,87,34),
			new SKColor(76,175,80),
			new SKColor(244,67,54)
		]);
	}

	public static byte[] Create(string text, int width, int height)
	{
		lock (_r)
		{
			var c = Colors[_r.Next(Colors.Count)];

			return Create(text, width, height, c);
		}
	}

	public static byte[] Create(string text, int width, int height, SKColor color)
	{
		if (text.Length > 2)
			text = text[..2];

		using var bitmap = new SKBitmap(width, height);
		using var canvas = new SKCanvas(bitmap);

		canvas.Clear(SKColor.Empty);

		var dark = new HslColor(color);
		dark.Darken();

		var paint = new SKPaint
		{
			Color = color
		};

		canvas.DrawRect(new SKRect(1, 1, 1, 1), paint);
		canvas.ClipRect(new SKRect(1, 1, 1, 1));
		canvas.DrawColor(color);

		using var typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold);

		var emSize = text.Length > 1 ? height / 3 : height / 2;

		using var f = new SKFont(typeface, emSize);
		using var fontPaint = new SKPaint();

		fontPaint.IsAntialias = true;
		fontPaint.Color = new SKColor(255, 255, 255);
		fontPaint.IsStroke = true;
		fontPaint.StrokeWidth = 3;

		canvas.DrawText(text, new SKPoint(width / 2f, height / 2), SKTextAlign.Center, f, paint);

		canvas.Flush();

		using var image = SKImage.FromBitmap(bitmap);
		using var data = image.Encode(SKEncodedImageFormat.Png, 100);

		using var ms = new MemoryStream();
		data.SaveTo(ms);
		ms.Position = 0;

		return ms.ToArray();
	}
}