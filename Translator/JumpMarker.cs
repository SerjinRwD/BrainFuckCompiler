using System.Reflection.Emit;

namespace BF
{
    internal struct JumpMarker
    {
        public Label CurrentPosLabel { get; set; }
        public int TrasitionPosId { get; set; }
    }
}
