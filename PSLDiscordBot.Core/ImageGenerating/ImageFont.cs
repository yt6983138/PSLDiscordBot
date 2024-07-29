using Newtonsoft.Json;
using SixLabors.Fonts;

namespace PSLDiscordBot.Core.ImageGenerating;
public class ImageFont
{
	private static readonly FontFamily DefaultFontFamily = SystemFonts.Collection.Families.ElementAt(0);
	private FontFamily _fontFamily;
	private Font? _font;
	private TextOptions? _options;

	public static ImageFont Default => new();

	public string FamilyName { get; set; } = "";
	public float FontSize { get; set; } = 12;
	public FontStyle Style { get; set; }

	[JsonIgnore]
	public FontFamily FontFamily => this._fontFamily == default ? this.CreateFont().Item1 : this._fontFamily;
	[JsonIgnore]
	public Font Font => this._font ?? this.CreateFont().Item2;
	[JsonIgnore]
	public TextOptions TextOptions => this._options ?? this.CreateFont().Item3;

	public (FontFamily, Font, TextOptions) CreateFont()
	{
		if (!SystemFonts.TryGet(this.FamilyName, out this._fontFamily))
			this._fontFamily = DefaultFontFamily;
		this._font = this._fontFamily.CreateFont(this.FontSize, this.Style);
		this._options = new(this._font);

		return (this._fontFamily, this._font, this._options);
	}
}
