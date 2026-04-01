var dynCall_vi = Module["dynCall_vi"] = function (cb, arg1) {
    getWasmTableEntry(cb)(arg1);
}
var dynCall_vii = Module["dynCall_vii"] = function (cb, arg1, arg2) {
    getWasmTableEntry(cb)(arg1, arg2);
}
var dynCall_viii = Module["dynCall_viii"] = function (cb, arg1, arg2, arg3) {
    getWasmTableEntry(cb)(arg1, arg2, arg3);
}
var dynCall_viiii = Module["dynCall_viiii"] = function (cb, arg1, arg2, arg3, arg4) {
    getWasmTableEntry(cb)(arg1, arg2, arg3, arg4);
}
var dynCall_iiii = Module["dynCall_iiii"] = function (cb, arg1, arg2, arg3) {
    return getWasmTableEntry(cb)(arg1, arg2, arg3);
}
