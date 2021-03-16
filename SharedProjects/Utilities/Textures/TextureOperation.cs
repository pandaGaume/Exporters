using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace Utilities
{

    public abstract class TextureOperation
    {
        string _name;

        public TextureOperation(string name)
        {
            _name = name ?? string.Empty;
        }

        public string Name => _name;
        public abstract void Apply(byte[] values, BitmapData infos);
    }

    public class FlipChannel : TextureOperation
    {
        public const int ChannelRed = 0;
        public const int ChannelGreen = 1;
        public const int ChannelBlue = 2;

        int _channel;

        /// <summary>
        /// Invert channel (R,G or B) using value = MAX_CHANNEL_VALUE - value.
        /// </summary>
        /// <param name="channel">R = 0, G = 1, B = 2</param>
        /// <param name="name"></param>
        /// <Exception name="ArgumentOutOfRangeException">channel value is invalid.</Exception>
        public FlipChannel(int channel, string name = null) : base(name ?? $"i{channel}")
        {
            if( channel < ChannelRed || channel > ChannelBlue)
            {
                throw new ArgumentOutOfRangeException(nameof(channel));
            }
            _channel = channel;
        }

        public override void Apply(byte[] values, BitmapData infos)
        {
            int pixelSize = Image.GetPixelFormatSize(infos.PixelFormat) >> 3;
            int i = 0;
            int l = values.Length;
            if (i < l)
            {
                switch (infos.PixelFormat)
                {
                    case PixelFormat.Canonical:
                    case PixelFormat.Format24bppRgb:
                    case PixelFormat.Format32bppRgb:
                        {
                            i += _channel;
                            do
                            {
                                values[i] = (byte)(0xFF - values[i]);
                                i += pixelSize;
                            } while (i < l);
                            break;
                        }
                    case PixelFormat.Format32bppArgb:
                    case PixelFormat.Format32bppPArgb:
                        {
                            i += (_channel + 1);
                            do
                            {
                                values[i] = (byte)(0xFF - values[i]);
                                i += pixelSize;
                            } while (i < l);
                            break;
                        }
                    default:
                        throw new NotSupportedException($"Pixel format not supported :{infos.PixelFormat}");
                }
            }
        }
    }

    public class SwapChannel : TextureOperation
    {
        int _channelA;
        int _channelB;
        /// <summary>
        /// Invert channel (R,G or B) using value = MAX_CHANNEL_VALUE - value.
        /// </summary>
        /// <param name="channel">R = 0, G = 1, B = 2</param>
        /// <param name="name"></param>
        /// <Exception name="ArgumentOutOfRangeException">channel value is invalid.</Exception>
        public SwapChannel(int channelA, int channelB, string name = null) : base(name ?? $"s{channelA}{channelB}")
        {
            if (channelA < 0 || channelA > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(channelA));
            }
            if (channelB < 0 || channelB > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(channelB));
            }
            _channelA = channelA;
            _channelB = channelB;
        }

        public override void Apply(byte[] values, BitmapData infos)
        {
            if(_channelA == _channelB)
            {
                return;
            }
            int pixelSize = Image.GetPixelFormatSize(infos.PixelFormat) >> 3;
            int i = 0;
            int j = 0;
            int l = values.Length;
            if (i < l)
            {
                switch (infos.PixelFormat)
                {
                    case PixelFormat.Canonical:
                    case PixelFormat.Format24bppRgb:
                    case PixelFormat.Format32bppRgb:
                        {
                            i += _channelA;
                            j += _channelB;
                            do
                            {
                                byte tmp = values[i];
                                values[i] = values[j];
                                values[j] = tmp;
                                i += pixelSize;
                                j += pixelSize;
                            } while (i < l);
                            break;
                        }
                    case PixelFormat.Format32bppArgb:
                    case PixelFormat.Format32bppPArgb:
                        {
                            i += (_channelA + 1);
                            j += (_channelB + 1);
                            do
                            {
                                byte tmp = values[i];
                                values[i] = values[j];
                                values[j] = tmp;
                                i += pixelSize;
                                j += pixelSize;
                            } while (i < l);
                            break;
                        }
                    default:
                        throw new NotSupportedException($"Pixel format not supported :{infos.PixelFormat}");
                }
            }
        }
    }

    public class InvertY : TextureOperation
    {
        public InvertY(string name = null) : base(name ?? "fy")
        {
        }

        public override void Apply(byte[] values, BitmapData infos)
        {
            int stride = infos.Stride;
            int from = 0;
            int to = (infos.Height - 1) * stride;

            if (from < to)
            {
                byte[] tmp = new byte[infos.Stride];
                do
                {
                    Array.Copy(values, to, tmp, 0, infos.Stride);
                    Array.Copy(values, from, values, to, infos.Stride);
                    Array.Copy(tmp, 0, values, from, infos.Stride);
                    from += stride;
                    to -= stride;
                } while (from < to);
            }
        }
    }

    public class Normalize : TextureOperation
    {
        public Normalize(string name = null) : base(name ?? "N")
        {
        }

        public override void Apply(byte[] values, BitmapData infos)
        {
            int pixelSize = Image.GetPixelFormatSize(infos.PixelFormat)>>3;
            int i = 0;
            int l = values.Length;
            if (i < l)
            {
                switch (infos.PixelFormat)
                {
                    case PixelFormat.Canonical:
                    case PixelFormat.Format24bppRgb:
                    case PixelFormat.Format32bppRgb:
                        {
                            do
                            {
                                var k = i;
                                int r = values[k++];
                                int g = values[k++];
                                int b = values[k];
                                var n = Math.Sqrt(r * r + g * g + b * b);
                                values[k--] = (byte)(b / n);
                                values[k--] = (byte)(g / n);
                                values[k] = (byte)(r / n);
                                i += pixelSize;
                            } while (i < l);
                            break;
                        }
                    case PixelFormat.Format32bppArgb:
                    case PixelFormat.Format32bppPArgb:
                        {
                            do
                            {
                                var k = i+1;
                                int r = values[k++];
                                int g = values[k++];
                                int b = values[k];
                                var n = Math.Sqrt(r * r + g * g + b * b);
                                values[k--] = (byte)(b / n);
                                values[k--] = (byte)(g / n);
                                values[k] = (byte)(r / n);
                                i += pixelSize;
                            } while (i < l);
                            break;
                        }
                    default:
                        throw new NotSupportedException($"Pixel format not supported :{infos.PixelFormat}");
                }
            }
        }
    }
}
