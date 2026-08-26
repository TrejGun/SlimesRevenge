using UnityEngine;

namespace SlimesRevenge
{
    public sealed class PlayerInput : MonoBehaviour
    {
        private static readonly KeyCode[] MoveKeys =
        {
            KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D,
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
            KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
            KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9,
            KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3,
            KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6,
            KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9
        };

        [SerializeField] private TurnManager turns;
        [SerializeField] private WorldView worldView;
        [SerializeField] private Slime slime;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private float swipePixels = 48f;

        private CombatMenu menu;
        private bool tracking;
        private bool fromSlime;
        private Vector2 pressScreen;

        private void Awake()
        {
            menu = GetComponent<CombatMenu>();
            if (menu == null)
            {
                menu = gameObject.AddComponent<CombatMenu>();
            }
        }

        private void Update()
        {
            if (turns == null || !turns.IsWaitingForInput || slime == null)
            {
                return;
            }

            if (menu != null && menu.BlocksInput)
            {
                return;
            }

            if (ReadKeyboard())
            {
                return;
            }

            if (Input.touchCount > 0)
            {
                ReadTouch(Input.GetTouch(0));
                return;
            }

            ReadMouse();
        }

        private bool ReadKeyboard()
        {
            foreach (var key in MoveKeys)
            {
                if (!Input.GetKeyDown(key) || !GridStep.TryFromKey(key, out var offset, out var wait))
                {
                    continue;
                }

                if (wait)
                {
                    turns.TryWait();
                }
                else
                {
                    turns.TryStep(offset);
                }

                return true;
            }

            return false;
        }

        private void ReadTouch(Touch touch)
        {
            if (touch.phase == TouchPhase.Began)
            {
                BeginPointer(touch.position);
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                MovePointer(touch.position);
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                EndPointer(touch.position);
            }
        }

        private void ReadMouse()
        {
            if (Input.GetMouseButtonDown(0))
            {
                BeginPointer(Input.mousePosition);
                return;
            }

            if (tracking && Input.GetMouseButton(0))
            {
                MovePointer(Input.mousePosition);
                return;
            }

            if (tracking && Input.GetMouseButtonUp(0))
            {
                EndPointer(Input.mousePosition);
            }
        }

        private void BeginPointer(Vector2 screen)
        {
            if (!TryCellAt(screen, out var cell))
            {
                tracking = false;
                return;
            }

            tracking = true;
            fromSlime = cell == slime.Cell;
            pressScreen = screen;
            SetSelected(fromSlime);
            SetHighlight(cell);
        }

        private void MovePointer(Vector2 screen)
        {
            if (!tracking || !TryCellAt(screen, out var cell))
            {
                return;
            }

            SetHighlight(cell);
        }

        private void EndPointer(Vector2 screen)
        {
            if (!tracking)
            {
                return;
            }

            tracking = false;
            var delta = screen - pressScreen;
            if (fromSlime && delta.magnitude >= swipePixels)
            {
                turns.TryStep(GridStep.FromSwipe(delta));
                ClearSelection();
                return;
            }

            if (fromSlime && delta.magnitude < swipePixels)
            {
                OpenSelfMenu();
                ClearSelection();
                return;
            }

            if (TryCellAt(screen, out var cell) && GridStep.IsAdjacent(slime.Cell, cell))
            {
                if (turns.Session != null && turns.Session.IsOccupied(cell))
                {
                    menu.OpenActions(slime.Volume.UniqueKinds(), substance => turns.TryAttack(cell, substance));
                }
                else
                {
                    turns.TryMoveTo(cell);
                }
            }

            ClearSelection();
        }

        private void OpenSelfMenu()
        {
            if (turns.Session == null)
            {
                return;
            }

            var floor = turns.Session.World.Floor;
            var cell = slime.Cell;
            var puddle = floor.GetPuddle(cell);
            var corpses = floor.GetCorpses(cell);
            var canMess = puddle == null && slime.Volume.UnitCount > 0;
            var canCollect = puddle != null && slime.Volume.UnitCount < Volume.Capacity;
            var canDevour = !slime.Digestion.IsBusy && corpses.Count > 0;
            menu.OpenSelf(
                slime.Volume.UniqueKinds(),
                canMess,
                canCollect,
                canDevour ? corpses : System.Array.Empty<Creature>(),
                substance => turns.TryMakeMess(substance),
                () => turns.TryCollectPuddle(),
                index => turns.TryDevourCorpse(index));
        }

        private void SetHighlight(Vector2Int cell)
        {
            if (turns.Session == null)
            {
                return;
            }

            var show = cell == slime.Cell || GridStep.IsAdjacent(slime.Cell, cell);
            turns.Session.HighlightCell = show ? cell : (Vector2Int?)null;
        }

        private void SetSelected(bool selected)
        {
            if (slime != null)
            {
                slime.transform.localScale = Vector3.one * (selected ? 1.15f : 1f);
            }
        }

        private void ClearSelection()
        {
            SetSelected(false);
            if (turns.Session != null)
            {
                turns.Session.HighlightCell = null;
            }
        }

        private bool TryCellAt(Vector2 screen, out Vector2Int cell)
        {
            cell = default;
            if (worldCamera == null || worldView == null || worldView.Map == null)
            {
                return false;
            }

            var ray = worldCamera.ScreenPointToRay(screen);
            var ground = new Plane(Vector3.forward, Vector3.zero);
            if (!ground.Raycast(ray, out var distance))
            {
                return false;
            }

            var point = ray.GetPoint(distance);
            cell = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
            return worldView.Map.Contains(cell);
        }
    }
}
