#!/usr/bin/env bash
# ==============================================================================
# MuMu-Lite: Linux QEMU-KVM VirtIO Runner
# Boots the cleaned Android 12 image on Linux with native hardware acceleration
# ==============================================================================

set -euo pipefail

DISK_IMAGE="${1:-./clean_system.raw}"
RAM_MB="${2:-2048}"
SMP_CORES="${3:-2}"
ADB_PORT="${4:-5555}"

echo -e "\033[0;36m=========================================\033[0m"
echo -e "\033[0;36m   MuMu-Lite: Linux KVM Runner           \033[0m"
echo -e "\033[0;36m=========================================\033[0m"

if ! command -v qemu-system-x86_64 &> /dev/null; then
    echo -e "\033[0;31m[ERROR] qemu-system-x86_64 is not installed. Install via:\033[0m"
    echo "  sudo apt install qemu-system-x86 qemu-kvm -y"
    exit 1
fi

if [ ! -f "$DISK_IMAGE" ]; then
    echo -e "\033[0;33m[!] Disk image not found at: $DISK_IMAGE\033[0m"
    echo "Convert your system.vdi to raw format first using:"
    echo "  qemu-img convert -O raw system.vdi clean_system.raw"
    exit 1
fi

echo -e "\033[0;32m[*] Launching Android 12 under KVM VirtIO...\033[0m"
echo "  -> Disk:     $DISK_IMAGE"
echo "  -> RAM:      ${RAM_MB} MB"
echo "  -> CPU:      ${SMP_CORES} Cores"
echo "  -> ADB Port: localhost:${ADB_PORT}"

exec qemu-system-x86_64 \
    -enable-kvm \
    -m "$RAM_MB" \
    -smp "$SMP_CORES" \
    -cpu host \
    -drive file="$DISK_IMAGE",format=raw,if=virtio,cache=none \
    -vga virtio \
    -display sdl,gl=on \
    -netdev user,id=net0,hostfwd=tcp::"$ADB_PORT"-:5555 \
    -device virtio-net-pci,netdev=net0 \
    -device virtio-mouse-pci \
    -device virtio-keyboard-pci
