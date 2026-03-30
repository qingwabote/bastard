(function () {
    Module.dynCall_vi = function (cb, arg1) {
        return getWasmTableEntry(cb)(arg1);
    };
    Module.dynCall_vii = function (cb, arg1, arg2) {
        return getWasmTableEntry(cb)(arg1, arg2);
    }
    Module.dynCall_viii = function (cb, arg1, arg2, arg3) {
        return getWasmTableEntry(cb)(arg1, arg2, arg3);
    }
    Module.dynCall_viiii = function (cb, arg1, arg2, arg3, arg4) {
        return getWasmTableEntry(cb)(arg1, arg2, arg3, arg4);
    }
})();