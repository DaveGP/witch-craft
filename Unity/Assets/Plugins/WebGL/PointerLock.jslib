// Click-to-lock pointer handling for the browser build, driven by Utilities.WebPointerLock.
// requestPointerLock only runs inside a real mousedown handler, and any refusal is swallowed, so browsers
// that are strict about user gestures (Safari) never surface an error.
mergeInto(LibraryManager.library, {
	WitchCraft_SetPointerLockWanted: function (wanted) {
		var canvas = Module.canvas;

		if (!canvas.witchCraftPointerLock) {
			canvas.witchCraftPointerLock = { wanted: false };

			canvas.addEventListener('mousedown', function () {
				if (!canvas.witchCraftPointerLock.wanted || document.pointerLockElement === canvas)
					return;

				try {
					var request = canvas.requestPointerLock();

					if (request && request.catch)
						request.catch(function () {});
				} catch (error) {
				}
			});
		}

		canvas.witchCraftPointerLock.wanted = !!wanted;

		if (!wanted && document.pointerLockElement === canvas)
			document.exitPointerLock();
	}
});
