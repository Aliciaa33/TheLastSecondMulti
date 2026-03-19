using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

		// Mouse for UI Interation
		private bool _uiMode = false;
		private bool _inventoryInput = false;
		public bool IsUIMode => _uiMode;

		public void SetUIMode(bool uiMode)
		{
			_uiMode = uiMode;
			cursorLocked = !uiMode;
			cursorInputForLook = !uiMode;
			SetCursorState(cursorLocked);

			// 在 UI 模式下停止角色移动输入
			if (uiMode)
			{
				move = Vector2.zero;
				look = Vector2.zero;
				jump = false;
				sprint = false;
			}

			// 确保鼠标状态正确
			Cursor.lockState = uiMode ? CursorLockMode.None : CursorLockMode.Locked;
			Cursor.visible = uiMode;
		}

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			if (_uiMode) return;
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if (_uiMode) return;
			if (cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			if (_uiMode) return;
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			if (_uiMode) return;
			SprintInput(value.isPressed);
		}

		public void OnInventory(InputValue value)
		{
			_inventoryInput = value.isPressed;
		}
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		}

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}

		public bool GetInventoryInput()
		{
			bool input = _inventoryInput;
			_inventoryInput = false; // 消费输入，防止持续触发
			return input;
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			if (!_uiMode) // 只有在非 UI 模式下才锁定光标
			{
				SetCursorState(cursorLocked);
			}
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}

}