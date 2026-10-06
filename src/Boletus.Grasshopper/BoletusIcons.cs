using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Procedurally-drawn 24×24 component icons (no image files / embedded resources): each glyph is
    /// rendered on demand with GDI+ primitives so the whole icon set is reviewable C# and reproducible
    /// from source. Every accessor returns a <em>fresh</em> <see cref="Bitmap"/> — Grasshopper caches
    /// the icon per component itself, and handing one shared instance to many owners would let GH
    /// dispose one owner's copy out from under the others (the resx pattern returns a fresh clone for
    /// the same reason). These are deliberately simple, flat geometric glyphs, not illustrated art.
    /// </summary>
    internal static class BoletusIcons
    {
        // House style. Warm Boletus brown/orange accent + neutral dark grey structure, on a
        // transparent ground. Strokes are kept ≥ ~1.4px and shapes near the pixel grid so the
        // glyphs stay legible at palette size.
        private static readonly Color Accent = Color.FromArgb(0xC8, 0x74, 0x3C);
        private static readonly Color AccentDark = Color.FromArgb(0x9A, 0x57, 0x2D);
        private static readonly Color AccentMid = Color.FromArgb(0xB0, 0x66, 0x35);
        private static readonly Color Neutral = Color.FromArgb(0x3C, 0x3C, 0x3C);

        /// <summary>Create a 24×24 ARGB bitmap, clear it transparent, run <paramref name="draw"/>.</summary>
        private static Bitmap Render(Action<Graphics> draw)
        {
            var bmp = new Bitmap(24, 24, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                draw(g);
            }
            return bmp;
        }

        // ── Sources ──────────────────────────────────────────────────────────────────────────────
        public static Bitmap Tpms => Render(DrawTpms);
        public static Bitmap Primitive => Render(DrawPrimitive);
        public static Bitmap SegmentPrimitive => Render(DrawSegmentPrimitive);
        public static Bitmap AxialPrimitive => Render(DrawAxialPrimitive);
        public static Bitmap MeshToVolume => Render(DrawMeshToVolume);
        public static Bitmap Strut => Render(DrawStrut);

        // ── Decorators ───────────────────────────────────────────────────────────────────────────
        public static Bitmap Onion => Render(DrawOnion);
        public static Bitmap GradedOnion => Render(DrawGradedOnion);
        public static Bitmap GradedOffset => Render(DrawGradedOffset);
        public static Bitmap Mix => Render(DrawMix);
        public static Bitmap Normalize => Render(DrawNormalize);
        public static Bitmap Transform => Render(DrawTransform);
        public static Bitmap Offset => Render(DrawOffset);
        public static Bitmap Twist => Render(DrawTwist);
        public static Bitmap Bend => Render(DrawBend);
        public static Bitmap Displace => Render(DrawDisplace);

        // ── Booleans ─────────────────────────────────────────────────────────────────────────────
        public static Bitmap Boolean => Render(DrawBoolean);

        // ── Terminals ────────────────────────────────────────────────────────────────────────────
        public static Bitmap ContourExport => Render(DrawContourExport);
        public static Bitmap ProxyPreview => Render(DrawProxyPreview);
        public static Bitmap LivePreview => Render(DrawLivePreview);

        // ── Param + assembly ─────────────────────────────────────────────────────────────────────
        public static Bitmap Volume => Render(DrawVolume);
        public static Bitmap Boletus => Render(DrawBoletus);

        // ── Glyph drawing routines ───────────────────────────────────────────────────────────────

        /// <summary>TPMS: two interleaved sine waves (a gyroid-like periodic lattice).</summary>
        private static void DrawTpms(Graphics g)
        {
            using var p1 = new Pen(Accent, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var p2 = new Pen(Neutral, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawCurve(p1, Sine(3f, 12f, 5.5f, 0f));
            g.DrawCurve(p2, Sine(3f, 12f, 5.5f, (float)Math.PI));
        }

        private static PointF[] Sine(float x0, float yMid, float amp, float phase)
        {
            var pts = new PointF[19];
            for (int i = 0; i < pts.Length; i++)
            {
                float x = x0 + i;                                   // 3 .. 21
                float t = (i / 18f) * (float)(2 * Math.PI * 1.5) + phase; // 1.5 periods
                pts[i] = new PointF(x, yMid + amp * (float)Math.Sin(t));
            }
            return pts;
        }

        /// <summary>Primitive: an isometric cube (three shaded faces) — the canonical clip solid.</summary>
        private static void DrawPrimitive(Graphics g)
        {
            PointF T = new(12, 3.5f), R = new(20.5f, 8f), L = new(3.5f, 8f), G = new(12, 12.5f),
                   Rp = new(20.5f, 16.5f), Lp = new(3.5f, 16.5f), Gp = new(12, 21f);

            using (var fTop = new SolidBrush(Accent))
            using (var fLeft = new SolidBrush(AccentDark))
            using (var fRight = new SolidBrush(AccentMid))
            {
                g.FillPolygon(fTop, new[] { T, R, G, L });
                g.FillPolygon(fLeft, new[] { L, G, Gp, Lp });
                g.FillPolygon(fRight, new[] { G, R, Rp, Gp });
            }

            using var pen = new Pen(Neutral, 1.5f) { LineJoin = LineJoin.Round };
            g.DrawPolygon(pen, new[] { T, R, Rp, Gp, Lp, L });
            g.DrawLine(pen, T, G);
            g.DrawLine(pen, L, G);
            g.DrawLine(pen, R, G);
            g.DrawLine(pen, G, Gp);
        }

        /// <summary>Segment Primitive: a diagonal capsule with its two end points — a solid spanned
        /// between A and B, distinct from the Primitive cube.</summary>
        private static void DrawSegmentPrimitive(Graphics g)
        {
            using (var body = new Pen(Accent, 8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawLine(body, 7f, 17f, 17f, 7f);
            using (var axis = new Pen(Neutral, 1.4f))
                g.DrawLine(axis, 4f, 20f, 20f, 4f);
            using var node = new SolidBrush(Neutral);
            g.FillEllipse(node, 2f, 18f, 4f, 4f);
            g.FillEllipse(node, 18f, 2f, 4f, 4f);
        }

        /// <summary>Axial Primitive: a cone standing on its vertical axis — the shapes placed by one
        /// point in their own frame.</summary>
        private static void DrawAxialPrimitive(Graphics g)
        {
            PointF apex = new(12, 3f), l = new(4f, 18f), r = new(20f, 18f);
            using (var side = new SolidBrush(AccentMid))
                g.FillPolygon(side, new[] { apex, r, l });
            using (var cap = new SolidBrush(Accent))
                g.FillEllipse(cap, 4f, 15.5f, 16f, 5f);
            using var pen = new Pen(Neutral, 1.4f) { DashStyle = DashStyle.Dash };
            g.DrawLine(pen, 12f, 1.5f, 12f, 22.5f);
        }

        /// <summary>Mesh → Volume: a triangulated patch, an arrow, and a soft field blob.</summary>
        private static void DrawMeshToVolume(Graphics g)
        {
            PointF a = new(2.5f, 5f), b = new(11f, 5f), c = new(2.5f, 15.5f), d = new(11f, 15.5f);
            using (var pen = new Pen(Neutral, 1.4f) { LineJoin = LineJoin.Round })
            {
                g.DrawPolygon(pen, new[] { a, b, d, c });
                g.DrawLine(pen, a, d);
                g.DrawLine(pen, b, c);
            }

            using (var arrow = new Pen(Neutral, 1.5f) { CustomEndCap = new AdjustableArrowCap(2.5f, 2.5f) })
                g.DrawLine(arrow, 12.2f, 10.25f, 15f, 10.25f);

            using var blob = new SolidBrush(Accent);
            g.FillEllipse(blob, 15.5f, 5.5f, 7.5f, 9.5f);
        }

        /// <summary>Strut lattice: a bcc-style unit cell — four corner nodes wired to a centre node
        /// (the wireframe-crystal source), distinct from the TPMS wave and the Primitive solid.</summary>
        private static void DrawStrut(Graphics g)
        {
            PointF tl = new(5, 5), tr = new(19, 5), bl = new(5, 19), br = new(19, 19), c = new(12, 12);

            // Cell outline (neutral) + centre-to-corner struts (accent) = body-centred cubic.
            using (var cell = new Pen(Color.FromArgb(0xB0, Neutral), 1.3f) { LineJoin = LineJoin.Round })
                g.DrawPolygon(cell, new[] { tl, tr, br, bl });
            using (var strut = new Pen(Accent, 1.9f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(strut, c, tl);
                g.DrawLine(strut, c, tr);
                g.DrawLine(strut, c, bl);
                g.DrawLine(strut, c, br);
            }

            // Nodes.
            using var node = new SolidBrush(Neutral);
            foreach (var p in new[] { tl, tr, bl, br }) g.FillEllipse(node, p.X - 1.6f, p.Y - 1.6f, 3.2f, 3.2f);
            using var hub = new SolidBrush(AccentDark);
            g.FillEllipse(hub, c.X - 2f, c.Y - 2f, 4f, 4f);
        }

        /// <summary>Graded offset: three parallel struts whose thickness grows across the glyph — a
        /// solid strut radius graded by a control field (vs. GradedOnion's hollow rings).</summary>
        private static void DrawGradedOffset(Graphics g)
        {
            // Struts run left→right, thin (radius) → thick (radius + t) — a graded SOLID radius.
            float[] ys = { 6.5f, 12f, 17.5f };
            float[] w0 = { 1.2f, 1.6f, 2.0f };   // left half-thickness
            float[] w1 = { 3.0f, 3.6f, 4.2f };   // right half-thickness
            using var cap = new SolidBrush(Accent);
            for (int i = 0; i < ys.Length; i++)
            {
                // Draw as a filled trapezoid so the thickness visibly ramps along the strut.
                float y = ys[i];
                g.FillPolygon(cap, new[]
                {
                    new PointF(3, y - w0[i]), new PointF(21, y - w1[i]),
                    new PointF(21, y + w1[i]), new PointF(3, y + w0[i]),
                });
            }
        }

        /// <summary>Mix: a thin crystal cell (A) morphing into a thick one (B) with an arrow between —
        /// a value-lerp morph across a control ramp.</summary>
        private static void DrawMix(Graphics g)
        {
            // Left cell "A" (thin struts, neutral) and right cell "B" (thick struts, accent).
            DrawMiniCell(g, cx: 6.5f, r: 4f, new Pen(Neutral, 1.4f), nodeR: 1.3f, Neutral);
            DrawMiniCell(g, cx: 17.5f, r: 4f, new Pen(Accent, 2.4f), nodeR: 1.8f, AccentDark);

            using var arrow = new Pen(Color.FromArgb(0xC0, Neutral), 1.4f)
            { CustomEndCap = new AdjustableArrowCap(2.2f, 2.2f) };
            g.DrawLine(arrow, 10f, 12f, 14f, 12f);
        }

        /// <summary>A tiny bcc cell centred at (<paramref name="cx"/>,12) for the Mix glyph.</summary>
        private static void DrawMiniCell(Graphics g, float cx, float r, Pen strut, float nodeR, Color nodeColor)
        {
            const float cy = 12f;
            PointF c = new(cx, cy);
            PointF[] corners =
            {
                new(cx - r, cy - r), new(cx + r, cy - r), new(cx - r, cy + r), new(cx + r, cy + r),
            };
            strut.StartCap = LineCap.Round;
            strut.EndCap = LineCap.Round;
            using (strut)
                foreach (var k in corners) g.DrawLine(strut, c, k);
            using var node = new SolidBrush(nodeColor);
            foreach (var k in corners) g.FillEllipse(node, k.X - nodeR, k.Y - nodeR, nodeR * 2, nodeR * 2);
        }

        /// <summary>Onion: three concentric rings around a hollow centre (a shell).</summary>
        private static void DrawOnion(Graphics g)
        {
            using var pa = new Pen(Accent, 2f);
            using var pn = new Pen(Neutral, 1.6f);
            g.DrawEllipse(pa, 3, 3, 18, 18);
            g.DrawEllipse(pn, 6.5f, 6.5f, 11, 11);
            g.DrawEllipse(pn, 10, 10, 4, 4);
        }

        /// <summary>Graded onion: concentric rings whose stroke thickens outward (graded thickness).</summary>
        private static void DrawGradedOnion(Graphics g)
        {
            using (var p1 = new Pen(Accent, 1f)) g.DrawEllipse(p1, 10, 10, 4, 4);
            using (var p2 = new Pen(Accent, 1.8f)) g.DrawEllipse(p2, 7, 7, 10, 10);
            using (var p3 = new Pen(Accent, 2.8f)) g.DrawEllipse(p3, 3, 3, 18, 18);
        }

        /// <summary>Normalize: equal-length radial ticks around a circle (unit gradient).</summary>
        private static void DrawNormalize(Graphics g)
        {
            using var pn = new Pen(Neutral, 1.6f);
            g.DrawEllipse(pn, 8.5f, 8.5f, 7, 7);

            using var pa = new Pen(Accent, 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            const float cx = 12f, cy = 12f;
            for (int i = 0; i < 8; i++)
            {
                double ang = i * Math.PI / 4;
                float cos = (float)Math.Cos(ang), sin = (float)Math.Sin(ang);
                g.DrawLine(pa, cx + cos * 6.5f, cy + sin * 6.5f, cx + cos * 9.5f, cy + sin * 9.5f);
            }
        }

        /// <summary>Transform: a dashed source box and a solid affine-warped target box with an arrow.</summary>
        private static void DrawTransform(Graphics g)
        {
            using (var dp = new Pen(Neutral, 1.3f) { DashStyle = DashStyle.Dash })
                g.DrawRectangle(dp, 3, 8, 9, 9);

            using (var sp = new Pen(Accent, 1.8f) { LineJoin = LineJoin.Round })
                g.DrawPolygon(sp, new[]
                {
                    new PointF(13, 5), new PointF(21, 7.5f), new PointF(18.5f, 15.5f), new PointF(10.5f, 13)
                });

            using var arrow = new Pen(Neutral, 1.4f) { CustomEndCap = new AdjustableArrowCap(2.3f, 2.3f) };
            g.DrawLine(arrow, 10.5f, 12.5f, 13.5f, 10f);
        }

        /// <summary>Offset: a neutral source outline grown to a larger accent outline (level-set shift).</summary>
        private static void DrawOffset(Graphics g)
        {
            using (var pn = new Pen(Neutral, 1.5f) { DashStyle = DashStyle.Dash })
                g.DrawEllipse(pn, 8, 8, 8, 8);
            using (var pa = new Pen(Accent, 1.9f))
                g.DrawEllipse(pa, 3.5f, 3.5f, 17, 17);
        }

        /// <summary>Twist: stacked rungs rotating around a vertical axis (a helical twist).</summary>
        private static void DrawTwist(Graphics g)
        {
            using var pn = new Pen(Neutral, 1.3f);
            g.DrawLine(pn, 12, 3, 12, 21);

            using var pa = new Pen(Accent, 1.9f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            // Rungs whose half-width shrinks then grows — the foreshortening of a turning bar.
            float[] ys = { 5f, 9f, 12f, 15f, 19f };
            float[] hw = { 7f, 4f, 1.5f, 4f, 7f };
            for (int i = 0; i < ys.Length; i++)
                g.DrawLine(pa, 12 - hw[i], ys[i], 12 + hw[i], ys[i]);
        }

        /// <summary>Bend: a straight baseline warped into an accent arc (circular-arc warp).</summary>
        private static void DrawBend(Graphics g)
        {
            using (var pn = new Pen(Neutral, 1.3f) { DashStyle = DashStyle.Dash })
                g.DrawLine(pn, 4, 20, 20, 20);
            using (var pa = new Pen(Accent, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawArc(pa, 2, 4, 26, 26, 200, 80);
        }

        /// <summary>Displace: a flat baseline and an accent wavy surface above it (additive bump).</summary>
        private static void DrawDisplace(Graphics g)
        {
            using (var pn = new Pen(Neutral, 1.3f) { DashStyle = DashStyle.Dash })
                g.DrawLine(pn, 3, 16, 21, 16);

            using var pa = new Pen(Accent, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            var pts = new PointF[]
            {
                new PointF(3, 9), new PointF(6, 5), new PointF(9, 9), new PointF(12, 5),
                new PointF(15, 9), new PointF(18, 5), new PointF(21, 9),
            };
            g.DrawCurve(pa, pts, 0.4f);
        }

        /// <summary>Boolean: two overlapping circles with the intersection lens filled (CSG).</summary>
        private static void DrawBoolean(Graphics g)
        {
            var r1 = new RectangleF(3, 6, 13, 13);
            var r2 = new RectangleF(8, 6, 13, 13);

            using (var path1 = new GraphicsPath())
            using (var path2 = new GraphicsPath())
            {
                path1.AddEllipse(r1);
                path2.AddEllipse(r2);
                using var lens = new Region(path1);
                lens.Intersect(path2);
                using var fill = new SolidBrush(Accent);
                g.FillRegion(fill, lens);
            }

            using var pn = new Pen(Neutral, 1.6f);
            g.DrawEllipse(pn, r1.X, r1.Y, r1.Width, r1.Height);
            g.DrawEllipse(pn, r2.X, r2.Y, r2.Width, r2.Height);
        }

        /// <summary>Contour / Export: an iso-curve over a grid with a downward export arrow.</summary>
        private static void DrawContourExport(Graphics g)
        {
            using (var grid = new Pen(Color.FromArgb(0xB0, Neutral), 1f))
            {
                for (int i = 0; i <= 3; i++) { float x = 4 + i * 5f; g.DrawLine(grid, x, 3, x, 12); }
                for (int i = 0; i <= 2; i++) { float y = 3 + i * 4.5f; g.DrawLine(grid, 4, y, 19, y); }
            }

            using (var curve = new Pen(Accent, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawCurve(curve, new[]
                {
                    new PointF(4, 10), new PointF(9, 5), new PointF(14, 9), new PointF(19, 4)
                });

            using var arrow = new Pen(Neutral, 1.8f) { CustomEndCap = new AdjustableArrowCap(2.8f, 2.8f) };
            g.DrawLine(arrow, 12, 14, 12, 21);
        }

        /// <summary>Proxy preview: a coarse low-poly facet ball (a coarse LOD).</summary>
        private static void DrawProxyPreview(Graphics g)
        {
            const float cx = 12f, cy = 12f, r = 9f;
            const int n = 7;
            var pts = new PointF[n];
            for (int i = 0; i < n; i++)
            {
                double a = -Math.PI / 2 + i * 2 * Math.PI / n;
                pts[i] = new PointF(cx + (float)Math.Cos(a) * r, cy + (float)Math.Sin(a) * r);
            }

            using (var fill = new SolidBrush(Color.FromArgb(0x40, Accent)))
                g.FillPolygon(fill, pts);

            using (var facet = new Pen(Color.FromArgb(0xA0, Neutral), 1f))
            {
                var hub = new PointF(cx + 1.5f, cy - 1.5f);
                foreach (var pt in pts) g.DrawLine(facet, hub, pt);
            }

            using var pn = new Pen(Neutral, 1.4f) { LineJoin = LineJoin.Round };
            g.DrawPolygon(pn, pts);
        }

        /// <summary>Live preview: an external monitor window showing a raymarched sphere — the GPU
        /// side-car window (distinct from ProxyPreview's coarse in-Rhino facet ball).</summary>
        private static void DrawLivePreview(Graphics g)
        {
            // Monitor bezel + stand.
            using (var screen = new SolidBrush(Color.FromArgb(0x22, Neutral)))
                g.FillRectangle(screen, 3, 4, 18, 13);
            using (var bezel = new Pen(Neutral, 1.5f) { LineJoin = LineJoin.Round })
                g.DrawRectangle(bezel, 3, 4, 18, 13);
            using (var stand = new Pen(Neutral, 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(stand, 12, 17, 12, 20);
                g.DrawLine(stand, 8.5f, 20.5f, 15.5f, 20.5f);
            }

            // Raymarched sphere inside the screen: filled disc + a specular highlight.
            using (var lit = new SolidBrush(Accent))
                g.FillEllipse(lit, 8, 6.5f, 8, 8);
            using (var shade = new SolidBrush(AccentDark))
                g.FillPie(shade, 8, 6.5f, 8, 8, 40, 160);
            using (var spec = new SolidBrush(Color.FromArgb(0xC0, Color.White)))
                g.FillEllipse(spec, 10, 8, 2.4f, 2.4f);
        }

        /// <summary>Volume (wire param): a soft field blob badged with a bold "V".</summary>
        private static void DrawVolume(Graphics g)
        {
            using (var fill = new SolidBrush(Color.FromArgb(0x30, Accent)))
                g.FillEllipse(fill, 3, 4, 18, 16);
            using (var pa = new Pen(Accent, 1.6f))
                g.DrawEllipse(pa, 3, 4, 18, 16);

            using var font = new Font("Arial", 9f, FontStyle.Bold, GraphicsUnit.Pixel);
            using var text = new SolidBrush(Neutral);
            using var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("V", font, text, new RectangleF(3, 4, 18, 16), fmt);
        }

        /// <summary>Assembly icon: a minimal boletus mushroom (cap dome + stem).</summary>
        private static void DrawBoletus(Graphics g)
        {
            using (var cap = new SolidBrush(Accent))
                g.FillPie(cap, 3, 4, 18, 16, 180, 180);      // top dome
            using (var underside = new Pen(AccentDark, 1.4f))
                g.DrawLine(underside, 3.5f, 12, 20.5f, 12);

            using (var stem = new SolidBrush(Color.FromArgb(0xE8, 0xD3, 0xB0)))
                g.FillRectangle(stem, 9.5f, 12, 5, 8);
            using var pn = new Pen(Neutral, 1.2f);
            g.DrawRectangle(pn, 9.5f, 12, 5, 8);
        }
    }
}
