
using UnityEngine;
using UnityEngine.EventSystems;

namespace Automation.Runtime
{
    
    public class StandaloneExtendedInputModule : StandaloneInputModule
    {
        
        //---------------------------------------------------------------------
        public void Tap(Vector2Int position)
        {
            Input.simulateMouseWithTouches = true;

            Touch touch = new Touch()
            {
                position = position,
            };
            
            PointerEventData pointerData = GetTouchPointerEventData(touch, out bool pressed, out bool released);
            
            ProcessTouchPress(pointerData, true, true);
        }
        
    }
    
}