
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.ComponentModel;
// a_pess&yahoo.com
// omar amin ibrahim
// coding for fun
// OCtober 31, 2008
// dedicated to Bob
namespace ScreenSaver
{

    public enum AnimationTypes
    {

        // sliding effect, 8 effects
        [Description("Left To Right")]
        LeftToRight,
        [Description("Righ To Left")]
        RighTotLeft,
        [Description("Top To Down")]
        TopToDown,
        [Description("Down To Top")]
        DownToTop,
        [Description("Top Left To Bottom Right")]
        TopLeftToBottomRight,
        [Description("Bottom Right To Top Left")]
        BottomRightToTopLeft,
        [Description("Bottom Left To Top Right")]
        BottomLeftToTopRight,
        [Description("Top Right To Bottom Left")]
        TopRightToBottomLeft,

        // rotating effects
        [Description("Maximize")]
        Maximize,
        [Description("Rotate")]
        Rotate,
        [Description("Spin from top Left")]
        SpinTopLeft,
        [Description("Spin from center")]
        SpinCenter,

        // shape effect , 3 effects
        [Description("Circular")]
        Circular,
        [Description("Elliptical")]
        Elliptical,
        [Description("Rectangular")]
        Rectangular,

        // split effect , 4 effects
        [Description("Split Horizontal")]
        SplitHorizontal,
        [Description("Split Vertical")]
        SplitVertical,
        [Description("Split Boom")]
        SplitBoom,
        [Description("Split Quarter")]
        SplitQuarter,

        // chess effect , 3 effects
        [Description("Chess Board")]
        ChessBoard,
        [Description("Chess Horizontal")]
        ChessHorizontal,
        [Description("Chess Vertical")]
        ChessVertical,

        // panorama effect , 3 effects
        [Description("Panorama")]
        Panorama,
        [Description("Panorama Horizontal")]
        PanoramaHorizontal,
        [Description("Panorama Vertical")]
        PanoramaVertical,

        // spiral effect , 2 effects
        [Description("Spiral")]
        Spiral,
        [Description("Spiral Boom")]
        SpiralBoom,

        // fade effect , 2 effects
        [Description("Fade")]
        Fade,
        [Description("Fade 2 Images")]
        Fade2Images,

        //Use no animation
        None

    }
}