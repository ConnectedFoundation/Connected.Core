﻿using SkiaSharp;

namespace Connected.Identities.Graphics;
internal class HslColor
{
	private double _hue = 1.0;
	private double _saturation = 1.0;
	private double _luminosity = 1.0;

	private const double _scale = 240.0;

	public HslColor() { }

	public HslColor(SKColor color)
	{
		SetRGB(color.Red, color.Green, color.Blue);
	}

	public HslColor(int red, int green, int blue)
	{
		SetRGB(red, green, blue);
	}

	public HslColor(double hue, double saturation, double luminosity)
	{
		Hue = hue;
		Saturation = saturation;
		Luminosity = luminosity;
	}

	public double Hue
	{
		get { return _hue * _scale; }
		set { _hue = CheckRange(value / _scale); }
	}

	public double Saturation
	{
		get { return _saturation * _scale; }
		set { _saturation = CheckRange(value / _scale); }
	}

	public double Luminosity
	{
		get { return _luminosity * _scale; }
		set { _luminosity = CheckRange(value / _scale); }
	}

	private static double CheckRange(double value)
	{
		if (value < 0.0)
			value = 0.0;
		else if (value > 1.0)
			value = 1.0;
		return value;
	}

	public void Darken()
	{
		Luminosity /= 1.5d;
	}

	public void Lighten()
	{
		Luminosity *= 1.5d;
	}

	public override string ToString()
	{
		return $"H: {Hue:#0.##} S: {Saturation:#0.##} L: {Luminosity:#0.##}";
	}

	public string ToRGBString()
	{
		var color = (SKColor)this;

		return $"R: {color.Red:#0.##} G: {color.Green:#0.##} B: {color.Blue:#0.##}";
	}

	public static implicit operator SKColor(HslColor hslColor)
	{
		var r = 0d;
		var g = 0d;
		var b = 0d;

		if (hslColor._luminosity != 0)
		{
			if (hslColor._saturation == 0)
				r = g = b = hslColor._luminosity;
			else
			{
				var temp2 = GetTemp2(hslColor);
				var temp1 = 2.0 * hslColor._luminosity - temp2;

				r = GetColorComponent(temp1, temp2, hslColor._hue + 1.0 / 3.0);
				g = GetColorComponent(temp1, temp2, hslColor._hue);
				b = GetColorComponent(temp1, temp2, hslColor._hue - 1.0 / 3.0);
			}
		}

		return new SKColor((byte)(255 * r), (byte)(255 * g), (byte)(255 * b));
	}

	private static double GetColorComponent(double temp1, double temp2, double temp3)
	{
		temp3 = MoveIntoRange(temp3);

		if (temp3 < 1.0 / 6.0)
			return temp1 + (temp2 - temp1) * 6.0 * temp3;
		else if (temp3 < 0.5)
			return temp2;
		else if (temp3 < 2.0 / 3.0)
			return temp1 + ((temp2 - temp1) * ((2.0 / 3.0) - temp3) * 6.0);
		else
			return temp1;
	}

	private static double MoveIntoRange(double temp3)
	{
		if (temp3 < 0.0)
			temp3 += 1.0;
		else if (temp3 > 1.0)
			temp3 -= 1.0;

		return temp3;
	}

	private static double GetTemp2(HslColor hslColor)
	{
		double temp2;

		if (hslColor._luminosity < 0.5)
			temp2 = hslColor._luminosity * (1.0 + hslColor._saturation);
		else
			temp2 = hslColor._luminosity + hslColor._saturation - (hslColor._luminosity * hslColor._saturation);

		return temp2;
	}

	public static implicit operator HslColor(SKColor color)
	{
		color.ToHsl(out float hue, out float saturation, out float luminosity);

		var hslColor = new HslColor
		{
			_hue = hue / 360.0, // we store hue as 0-1 as opposed to 0-360 
			_luminosity = luminosity,
			_saturation = saturation
		};

		return hslColor;
	}

	public void SetRGB(int red, int green, int blue)
	{
		var hslColor = (HslColor)new SKColor((byte)red, (byte)green, (byte)blue);

		_hue = hslColor._hue;
		_saturation = hslColor._saturation;
		_luminosity = hslColor._luminosity;
	}
}