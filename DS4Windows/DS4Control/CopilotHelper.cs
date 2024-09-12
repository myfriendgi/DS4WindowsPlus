using Nefarius.ViGEm.Client;
using System;


// The CopilotHelper allows one controller to co-drive another controller.
// It was developed so parents can help young kids to play tircky games by doing
// some of the difficult stuff for them, like right stick camera navigation.
// It is also good to help or play with people who have a disability and
// would find a full controller difficult to use. It can be fun just to
// play together on a one player game, taking different controls, to make
// it two player. It is also convinient (and more hygenic) for some turn-based games,
// when you just prefer to have a controller each instead of passing one between you.
// Coded by Giles E-P, Sept 2024.
namespace DS4Windows
{
    public class CopilotHelper
    {
        int devDest = 0;
        int devSource = 1;
        DS4State stateSource = new DS4State();
        DS4State stateShared = new DS4State();
        DS4State stateUnused = new DS4State();
        IVirtualGamepad sourceVirtualGamepad;
        bool copilotEnabled = false;

        public static CopilotHelper Instance = new CopilotHelper();

        private CopilotHelper()
        {
        }

        // Called when anything to do with the copilot settings is changed.
        // Just sets the options locally and resets the source constrols.
        public void UpdateCopilotConfig(bool enabled, int source, int dest)
        {
            if (copilotEnabled != enabled || devDest != dest || devSource != source)
            {
                copilotEnabled = enabled;
                devDest = dest;
                devSource = source;

                stateUnused.CopyTo(stateSource);
            }
        }

        // Called when a controller is disconnected to check if it is the copilot source
        // and if so clear it. This is to stop any possible buttons or sticks on the
        // copilot source controller staying pressed or held when it disconnects.
        public void DisconnectController(IVirtualGamepad vpad)
        {
            if (copilotEnabled && vpad == sourceVirtualGamepad)
            {
                stateUnused.CopyTo(stateSource);
            }
        }


        // Returns the length of the stick vector. It is coded in a bespoke way because some controllers
        // (xbox) centre to 127, 127 and some (ds4) centre to 128, 128. Either way, the  code should set
        // fX and fY such that the centre is 0 and the range is from -127 to +127 (not to +128).
        double GetStickLength(byte x, byte y)
        {
            var fX = (int)(x - 127.5f);
            var fY = (int)(y - 127.5f);
            var result = Math.Round(Math.Sqrt(fX * fX + fY * fY), MidpointRounding.AwayFromZero);
            return result;
        }

        // This gets called by the source and dest controllers
        public DS4State UpdateState(DS4State state, IVirtualGamepad vpad, int device)
        {
            // If we are not using copilot, or this is not a relevant controller, just return the state untouched
            if (!copilotEnabled || (device != devDest && device != devSource) || (devDest == devSource))
            {
                return state;
            }

            if (device == devSource)
            {
                state.CopyTo(stateSource);
                sourceVirtualGamepad = vpad;   // We keep a reference for the Disconnect function
                return stateUnused;            // Source is now an unused (stationary) controller
            }

            // If we got here, we are processing the target controller (device == devDest).
            // So now we're just going to shove in all the shared stick and button values.

            DS4State stateTarget = state;   // gep todo: can this be more efficient, by using
            state.CopyTo(stateShared);      // state directly, not cloning it?

            stateShared.Share = stateTarget.Share | stateSource.Share;
            stateShared.L3 = stateTarget.L3 | stateSource.L3;
            stateShared.R3 = stateTarget.R3 | stateSource.R3;
            stateShared.Options = stateTarget.Options | stateSource.Options;

            stateShared.DpadUp = stateTarget.DpadUp | stateSource.DpadUp;
            stateShared.DpadRight = stateTarget.DpadRight | stateSource.DpadRight;
            stateShared.DpadDown = stateTarget.DpadDown | stateSource.DpadDown;
            stateShared.DpadLeft = stateTarget.DpadLeft | stateSource.DpadLeft;

            stateShared.L1 = stateTarget.L1 | stateSource.L1;
            stateShared.R1 = stateTarget.R1 | stateSource.R1;

            stateShared.L2 = Math.Max(stateTarget.L2, stateSource.L2);
            stateShared.R2 = Math.Max(stateTarget.R2, stateSource.R2);

            stateShared.Triangle = stateTarget.Triangle | stateSource.Triangle;
            stateShared.Cross = stateTarget.Cross | stateSource.Cross;
            stateShared.Square = stateTarget.Square | stateSource.Square;
            stateShared.Circle = stateTarget.Circle | stateSource.Circle;
            stateShared.PS = stateTarget.PS | stateSource.PS;

            // nice resolver for blending the shared controller's sticks weighted by relative distance

            var stickLengthTargetL = GetStickLength(stateTarget.LX, stateTarget.LY);
            var stickLengthTargetR = GetStickLength(stateTarget.RX, stateTarget.RY);
            var stickLengthSourceL = GetStickLength(stateSource.LX, stateSource.LY);
            var stickLengthSourceR = GetStickLength(stateSource.RX, stateSource.RY);

            var totalLengthL = stickLengthTargetL + stickLengthSourceL;
            var tL = totalLengthL == 0 ? 0 :  stickLengthTargetL / totalLengthL;
            if (tL > 1) tL = 1;
            stateShared.LX = (byte) double.Lerp(stateSource.LX, stateTarget.LX, tL);
            stateShared.LY = (byte) double.Lerp(stateSource.LY, stateTarget.LY, tL);

            var totalLengthR = stickLengthTargetR + stickLengthSourceR;
            var tR = totalLengthR == 0 ? 0 : stickLengthTargetR / totalLengthR;
            if (tR > 1) tR = 1;
            stateShared.RX = (byte) double.Lerp(stateSource.RX, stateTarget.RX, tR);
            stateShared.RY = (byte) double.Lerp(stateSource.RY, stateTarget.RY, tR);

            return stateShared;
        }
    }
}