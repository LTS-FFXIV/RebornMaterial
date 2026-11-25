using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Input;

namespace RebornMaterial;

internal sealed unsafe class M3GamepadBlocker : IDisposable
{
	private const long PadRetryMilliseconds = 1000;
	private const long PolledFreshMilliseconds = 250;

	private static M3GamepadBlocker? _instance;
	private static long _lastPadRetryAt;
	private static int _polledButtons;
	private static long _polledAt;

	private bool _movement;
	private bool _camera;
	private bool _buttons;

	[StructLayout(LayoutKind.Explicit, Size = 0x18)]
	private struct PlayerMoveControllerFlyInput
	{
		[FieldOffset(0x0)] public float Forward;
		[FieldOffset(0x4)] public float Left;
		[FieldOffset(0x8)] public float Up;
		[FieldOffset(0xC)] public float Turn;
		[FieldOffset(0x10)] public float Unknown10;
		[FieldOffset(0x14)] public byte DirMode;
		[FieldOffset(0x15)] public byte HaveBackwardOrStrafe;
	}

	private enum CameraControlType : uint
	{
		None = 0,
		Keyboard = 1,
		Gamepad = 2,
		Mouse = 3,
	}

	private delegate void WalkDelegate(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte additive);

	private delegate void FlyDelegate(void* self, PlayerMoveControllerFlyInput* result);

	private delegate CameraControlType CameraControlTypeDelegate();

	private delegate nint PadPollDelegate(PadDevice* self);

	[Signature("E8 ?? ?? ?? ?? 80 7B 3E 00 48 8D 3D", DetourName = nameof(WalkDetour))]
	private readonly Hook<WalkDelegate>? _walkHook = null;

	[Signature("E8 ?? ?? ?? ?? 0F B6 0D ?? ?? ?? ?? B8", DetourName = nameof(FlyDetour))]
	private readonly Hook<FlyDelegate>? _flyHook = null;

	[Signature("E8 ?? ?? ?? ?? 83 F8 01 74 5F", DetourName = nameof(CameraControlTypeDetour))]
	private readonly Hook<CameraControlTypeDelegate>? _cameraHook = null;

	private Hook<PadPollDelegate>? _padHook;

	public static bool IsEnabled { get; private set; }

	private M3GamepadBlocker()
	{
		M3Services.GameInterop.InitializeFromAttributes(this);
	}

	public static void Enable(bool movement, bool camera, bool buttons)
	{
		try
		{
			_instance ??= new M3GamepadBlocker();
			_instance._movement = movement;
			_instance._camera = camera;
			_instance._buttons = buttons;
			IsEnabled = _instance.Apply();
		}
		catch (Exception ex)
		{
			M3Services.Log?.Warning($"[RebornMaterial] Controller input blocking isn't available: {ex.Message}");
			Disable();
		}
	}

	public static void Disable()
	{
		if (_instance == null)
		{
			IsEnabled = false;
			return;
		}

		_instance._movement = false;
		_instance._camera = false;
		_instance._buttons = false;
		_instance._walkHook?.Disable();
		_instance._flyHook?.Disable();
		_instance._cameraHook?.Disable();
		_instance._padHook?.Disable();
		Volatile.Write(ref _polledAt, 0);
		IsEnabled = false;
	}

	public static void Dispose()
	{
		IsEnabled = false;
		Volatile.Write(ref _polledAt, 0);
		_lastPadRetryAt = 0;
		_instance?.DisposeHooks();
		_instance = null;
	}

	internal static void Tick()
	{
		if (_instance == null || !_instance._buttons || _instance._padHook != null)
		{
			return;
		}

		var now = Environment.TickCount64;
		if (now - _lastPadRetryAt < PadRetryMilliseconds)
		{
			return;
		}

		_lastPadRetryAt = now;
		try
		{
			_instance.EnsurePadHook();
			if (_instance._padHook != null)
			{
				_instance._padHook.Enable();
				IsEnabled = true;
			}
		}
		catch (Exception ex)
		{
			M3Services.Log?.Warning($"[RebornMaterial] Controller button hook failed: {ex.Message}");
		}
	}

	internal static bool TryReadButtons(out GamepadButtons buttons)
	{
		var polledAt = Volatile.Read(ref _polledAt);
		if (polledAt > 0 && Environment.TickCount64 - polledAt < PolledFreshMilliseconds)
		{
			buttons = (GamepadButtons)Volatile.Read(ref _polledButtons);
			return true;
		}

		buttons = GamepadButtons.None;
		return false;
	}

	void IDisposable.Dispose()
	{
		DisposeHooks();
	}

	private bool Apply()
	{
		if (_movement && (_walkHook == null || _flyHook == null))
		{
			M3Services.Log?.Warning("[RebornMaterial] One or more controller movement hooks could not be resolved.");
		}

		if (_camera && _cameraHook == null)
		{
			M3Services.Log?.Warning("[RebornMaterial] Controller camera hook could not be resolved.");
		}

		Toggle(_walkHook, _movement);
		Toggle(_flyHook, _movement);
		Toggle(_cameraHook, _camera);
		if (_buttons)
		{
			EnsurePadHook();
		}

		Toggle(_padHook, _buttons);
		return (_movement && (_walkHook != null || _flyHook != null))
			|| (_camera && _cameraHook != null)
			|| (_buttons && _padHook != null);
	}

	private static void Toggle<T>(Hook<T>? hook, bool enabled) where T : Delegate
	{
		if (hook == null)
		{
			return;
		}

		if (enabled)
		{
			hook.Enable();
		}
		else
		{
			hook.Disable();
		}
	}

	private void EnsurePadHook()
	{
		if (_padHook != null)
		{
			return;
		}

		var manager = InputDeviceManager.Instance();
		if (manager == null || manager->PadDevice == null)
		{
			return;
		}

		var vtable = *(nint**)manager->PadDevice;
		_padHook = M3Services.GameInterop.HookFromAddress<PadPollDelegate>(vtable[2], PadPollDetour);
	}

	private void DisposeHooks()
	{
		_walkHook?.Dispose();
		_flyHook?.Dispose();
		_cameraHook?.Dispose();
		_padHook?.Dispose();
		_padHook = null;
	}

	private void WalkDetour(void* self, float* sumLeft, float* sumForward, float* sumTurnLeft, byte* haveBackwardOrStrafe, byte* a6, byte additive)
	{
		_walkHook!.Original(self, sumLeft, sumForward, sumTurnLeft, haveBackwardOrStrafe, a6, additive);
		if (!M3Gamepad.ShouldBlockGame)
		{
			return;
		}

		*sumLeft = 0f;
		*sumForward = 0f;
		*sumTurnLeft = 0f;
		*haveBackwardOrStrafe = 0;
	}

	private void FlyDetour(void* self, PlayerMoveControllerFlyInput* result)
	{
		_flyHook!.Original(self, result);
		if (!M3Gamepad.ShouldBlockGame)
		{
			return;
		}

		result->Forward = 0f;
		result->Left = 0f;
		result->Up = 0f;
		result->Turn = 0f;
	}

	private CameraControlType CameraControlTypeDetour()
	{
		var type = _cameraHook!.Original();
		return type == CameraControlType.Gamepad && M3Gamepad.ShouldBlockGame ? CameraControlType.None : type;
	}

	private nint PadPollDetour(PadDevice* self)
	{
		var result = _padHook!.Original(self);
		ref var input = ref self->GamepadInputData;

		var polled = GamepadButtons.None;
		if (input.Square > 0) polled |= GamepadButtons.West;
		if (input.Cross > 0) polled |= GamepadButtons.South;
		if (input.Circle > 0) polled |= GamepadButtons.East;
		if (input.Triangle > 0) polled |= GamepadButtons.North;
		if (input.L1 > 0) polled |= GamepadButtons.L1;
		if (input.R1 > 0) polled |= GamepadButtons.R1;
		if (input.L2 > 0) polled |= GamepadButtons.L2;
		if (input.R2 > 0) polled |= GamepadButtons.R2;
		if (input.Start > 0) polled |= GamepadButtons.Start;
		if (input.Select > 0) polled |= GamepadButtons.Select;
		if (input.L3 > 0) polled |= GamepadButtons.L3;
		if (input.R3 > 0) polled |= GamepadButtons.R3;
		if (input.DPadLeft > 0) polled |= GamepadButtons.DpadLeft;
		if (input.DPadRight > 0) polled |= GamepadButtons.DpadRight;
		if (input.DPadUp > 0) polled |= GamepadButtons.DpadUp;
		if (input.DPadDown > 0) polled |= GamepadButtons.DpadDown;

		Volatile.Write(ref _polledButtons, (int)polled);
		Volatile.Write(ref _polledAt, Environment.TickCount64);

		if (!M3Gamepad.ShouldBlockGame)
		{
			return result;
		}

		input.Buttons = GamepadButtonsFlags.None;
		input.ButtonsPressed = GamepadButtonsFlags.None;
		input.ButtonsReleased = GamepadButtonsFlags.None;
		input.ButtonsRepeat = GamepadButtonsFlags.None;
		input.Square = 0;
		input.Cross = 0;
		input.Circle = 0;
		input.Triangle = 0;
		input.L1 = 0;
		input.R1 = 0;
		input.L2 = 0;
		input.R2 = 0;
		input.Start = 0;
		input.Select = 0;
		input.L3 = 0;
		input.R3 = 0;
		input.DPadLeft = 0;
		input.DPadRight = 0;
		input.DPadUp = 0;
		input.DPadDown = 0;
		return result;
	}
}
