// =====================================================================
// Copyright © 2023 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using JetBrains.Annotations;

namespace ToolBuddy.Converter.Common.Converters
{
    public abstract class BaseConverter<TSource, TDestination> where TSource : class
        where TDestination : class
    {
        /// <summary>
        /// Gets the name of the associated converter as it would be displayed on the UI.
        /// </summary>
        protected abstract string GetConverterId { get; }

        protected BaseConverter()
        {
            ConverterRegistry.RegisterConverter(
                GetConverterId
            );
        }

        /// <summary>
        /// Converts a source spline to a destination spline.
        /// </summary>
        /// <param name="source">The source spline to convert.</param>
        /// <param name="destination">The destination spline to populate.</param>
        public abstract void Convert(
            [NotNull] TSource source,
            [NotNull] TDestination destination);
    }
}