﻿using System;
using System.Collections.Generic;
using System.Linq;
using Artemis.Core;
using Artemis.Core.LayerBrushes;
using Artemis.Plugins.LayerBrushes.Nexus.LayerProperties;
using SkiaSharp;

namespace Artemis.Plugins.LayerBrushes.Nexus.LayerBrush
{
    public class NexusLayerBrush : LayerBrush<NexusLayerBrushProperties>
    {
        private List<SkBeam> _beams = new List<SkBeam>();
        private readonly Profiler _profiler;
        private object BeamsLock = new object();

        public Random Rand { get; set; }

        public NexusLayerBrush(Plugin plugin)
        {
            _profiler = plugin.GetProfiler("ShuffleLayerBrush");
        }

        public override void EnableLayerBrush()
        {
            Rand = new Random(Layer.EntityId.GetHashCode());
        }

        public override void DisableLayerBrush()
        {
        }

        double _spawnTime;

        public override void Update(double deltaTime)
        {
            if (deltaTime < 0)
                return;

            _profiler.StartMeasurement("Update");
            // Remove beams
            lock (_beams)
            {
                for (int i = _beams.Count - 1; i >= 0; i--)
                {
                    _beams[i].Move((float)deltaTime * _beams[i].Speed * 10);

                    switch (_beams[i].Direction)
                    {
                        case Direction.ToDown:
                        case Direction.ToUp:
                            if (_beams[i].Position > Layer.Bounds.Height + Properties.TrailSize)
                                _beams.RemoveAt(i);
                            break;
                        case Direction.ToRight:
                        case Direction.ToLeft:
                            if (_beams[i].Position > Layer.Bounds.Width + Properties.TrailSize)
                                _beams.RemoveAt(i);
                            break;
                    }
                }
                // Spawn a new beams
                _spawnTime += deltaTime;
                if (_spawnTime > Properties.SpawnInterval.CurrentValue / 1000f) // Time in MS
                {

                    Direction direction = SkBeam.GetRandomDirection
                        (
                        Properties.FromRightToLeft.CurrentValue,
                        Properties.FromBottomToUp.CurrentValue,
                        Properties.FromLeftToRight.CurrentValue,
                        Properties.FromTopToBottom.CurrentValue
                        );

                    int location;
                    switch (direction)
                    {
                        case Direction.ToDown:
                        case Direction.ToUp:
                            location = GetHorizontalIndex();
                            break;
                        case Direction.ToRight:
                        case Direction.ToLeft:
                            location = GetVerticalIndex();
                            break;
                        default:
                            location = 0;
                            break;
                    }

                    _beams.Add(new SkBeam(direction, location, Properties.Width.CurrentValue, GetColorsForNewBeam(), Properties.Speed.CurrentValue.GetRandomValue()));
                    _spawnTime = 0;
                }
            }
            _profiler.StopMeasurement("Update");
        }

        private int GetVerticalIndex()
        {
            if (Properties.AvoidOverlaping.CurrentValue)
            {
                var vIndexes = _beams.Where(b => b.Direction == Direction.ToLeft || b.Direction == Direction.ToRight).Select(b => b.Location).ToArray();
                var vMaxIndexes = Layer.Bounds.Height / (Properties.Width + Properties.Separation);
                return GetRandomNonOverlapedPositionIndex(0, (Layer.Bounds.Height / (Properties.Width + Properties.Separation)), vIndexes, vMaxIndexes) * (Properties.Width + Properties.Separation);
            }
            else
            {
                return Rand.Next(1, Layer.Bounds.Height);
            }
        }

        private int GetHorizontalIndex()
        {
            if (Properties.AvoidOverlaping.CurrentValue)
            {
                var hIndexes = _beams.Where(b => b.Direction == Direction.ToUp || b.Direction == Direction.ToDown).Select(b => b.Location).ToArray();
                var hMaxIndexes = Layer.Bounds.Width / (Properties.Width + Properties.Separation);
                return GetRandomNonOverlapedPositionIndex(0, (Layer.Bounds.Width / (Properties.Width + Properties.Separation)), hIndexes, hMaxIndexes) * (Properties.Width + Properties.Separation);
            }
            else
            {
                return Rand.Next(1, Layer.Bounds.Width);
            }
        }

        private int GetRandomNonOverlapedPositionIndex(int from, int to, int[] avoid, int retries = 10)
        {
            int r;
            for (int i = 0; i < retries * 10; i++)
            {
                r = Rand.Next(from, to);
                if (!avoid.Contains(r * (Properties.Width + Properties.Separation)))
                    return r;
            }

            // Return just a random to avoid lock
            return r = Rand.Next(from, to); ;
        }

        private ColorGradient GetColorsForNewBeam()
        {
            if (Properties.ColorMode.CurrentValue == ColorType.Random)
            {
                SKColor color = SKColor.FromHsv(Rand.Next(0, 360), 100, 100);
                ColorGradient colors = new ColorGradient();
                colors.Add(
                    new ColorGradientStop(
                        color.WithAlpha(0),
                        0
                        )
                    );

                colors.Add(
                    new ColorGradientStop(
                        color.WithAlpha(255),
                        1
                        )
                    );
                return colors;
            }
            else if (Properties.ColorMode.CurrentValue == ColorType.ColorSet)
            {
                SKColor color = Properties.Colors.CurrentValue.GetColor((float)Rand.NextDouble());
                ColorGradient colors = new ColorGradient();
                colors.Add(
                    new ColorGradientStop(
                        color.WithAlpha(0),
                        0
                        )
                    );

                colors.Add(
                    new ColorGradientStop(
                        color.WithAlpha(255),
                        1
                        )
                    );
                return colors;
            }
            else if (Properties.ColorMode.CurrentValue == ColorType.Solid)
            {
                SKColor color = Properties.Color.CurrentValue; ;
                ColorGradient colors = new ColorGradient();
                colors.Add(
                    new ColorGradientStop(
                        color.WithAlpha(0),
                        0
                        )
                    );

                colors.Add(
                    new ColorGradientStop(
                        color.WithAlpha(255),
                        1
                        )
                    );
                return colors;
            }
            else if (Properties.ColorMode.CurrentValue == ColorType.Gradient)
            {
                return Properties.Colors.CurrentValue;
            }
            else
            {
                return ColorGradient.GetUnicornBarf();
            }
        }

        private SKRect CreateToUpRect(SkBeam beam)
        {
            float x = beam.Location;
            float y = beam.Position;

            return new SKRect(
                x,
                Layer.Bounds.Height - y,
                x + beam.Width,
                (Layer.Bounds.Height - y) + Properties.TrailSize
                );
        }

        private SKRect CreateToDownRect(SkBeam beam)
        {
            float x = beam.Location;
            float y = beam.Position;

            return new SKRect(
                x,
                y - Properties.TrailSize,
                x + beam.Width,
                y
                );
        }

        private SKRect CreateToLeftRect(SkBeam beam)
        {
            float x = beam.Position;
            float y = beam.Location;

            return new SKRect(
                 Layer.Bounds.Width - x,
                 y,
                 (Layer.Bounds.Width - x) + Properties.TrailSize,
                 y + beam.Width
                );
        }

        private SKRect CreateToRightRect(SkBeam beam)
        {
            float x = beam.Position;
            float y = beam.Location;

            return new SKRect(
                 x - Properties.TrailSize,
                 y,
                 x,
                 y + beam.Width
                );
        }


        private SKShader CreateVerticalShader(SKRect rect, ColorGradient colors, bool inverted = false)
        {
            return SKShader.CreateLinearGradient
                                 (
                                    new SKPoint(rect.Left, inverted ? rect.Bottom : rect.Top),
                                    new SKPoint(rect.Left, inverted ? rect.Top : rect.Bottom),

                                    // Get from gradient
                                    colors.GetColorsArray(),
                                    colors.GetPositionsArray(),
                                    SKShaderTileMode.Clamp
                                );
        }

        private SKShader CreateHorizontalShader(SKRect rect, ColorGradient colors, bool inverted = false)
        {
            return SKShader.CreateLinearGradient
                                 (
                                    new SKPoint(inverted ? rect.Right : rect.Left, rect.Top),
                                    new SKPoint(inverted ? rect.Left : rect.Right, rect.Top),

                                    // Get from gradient
                                    colors.GetColorsArray(),
                                    colors.GetPositionsArray(),
                                    SKShaderTileMode.Clamp
                                );
        }

        public override void Render(SKCanvas canvas, SKRect bounds, SKPaint paint)
        {
            _profiler.StartMeasurement("Render");
            lock (_beams)
            {
                SKRect beamRect = SKRect.Empty;
                foreach (SkBeam beam in _beams)
                {
                    // Calculate the point bases in direction
                    switch (beam.Direction)
                    {
                        case Direction.ToDown:
                            beamRect = CreateToDownRect(beam);
                            paint.Shader = CreateVerticalShader(beamRect, beam.Colors);
                            break;
                        case Direction.ToUp:
                            beamRect = CreateToUpRect(beam);
                            paint.Shader = CreateVerticalShader(beamRect, beam.Colors, true);
                            break;
                        case Direction.ToLeft:
                            beamRect = CreateToLeftRect(beam);
                            paint.Shader = CreateHorizontalShader(beamRect, beam.Colors, true);
                            break;
                        case Direction.ToRight:
                            beamRect = CreateToRightRect(beam);
                            paint.Shader = CreateHorizontalShader(beamRect, beam.Colors);
                            break;
                        default:
                            break;
                    }

                    //Paint our beam

                    canvas.DrawRect(
                           beamRect,
                           paint
                           );
                }
            }
            _profiler.StopMeasurement("Render");
        }
    }
}