using System;

namespace com.ootii.AI.Controllers
{
    /// <summary>
    /// Defines the tooltip value for motion properties
    /// </summary>
    public class MotionTooltipAttribute : Attribute
    {
        /// <summary>
        /// Default tooltip value
        /// </summary>
        protected string mTooltip;
        public string Tooltip
        {
            get { return this.mTooltip; }
        }

        /// <summary>
        /// Constructor for the attribute
        /// </summary>
        /// <param name="rValue">Value that is the tooltip</param>
        public MotionTooltipAttribute(string rValue)
        {
            this.mTooltip = rValue;
        }
    }
}
