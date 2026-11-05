/*
 * Scalio.app'in universal başlatıcısı.
 *
 * ⚠️ NEDEN VAR: .NET'in tek dosya yayını universal (arm64 + x86_64) tek bir
 * ikili üretemiyor — iki tek dosya ikiliyi `lipo` ile birleştirmek, ikiliye
 * ekli paketin konumunu bozar. Bu yüzden paket iki mimarinin uygulamasını
 * yan yana taşır (`Contents/MacOS/arm64/`, `Contents/MacOS/x64/`) ve bu küçük
 * universal ikili, makinenin mimarisine uyanı `execv` ile başlatır.
 *
 * `execv` süreci DEĞİŞTİRİR, yeni süreç açmaz: Dock simgesi, PID ve
 * LaunchServices kaydı aynı kalır.
 *
 * Mimari derleme anında belirlenir: Apple Silicon'da arm64 dilimi çalışır;
 * kullanıcı "Rosetta ile aç" derse x86_64 dilimi çalışır ve x64 sürümü açılır.
 */
#include <libgen.h>
#include <limits.h>
#include <mach-o/dyld.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>

#if defined(__arm64__)
#define SCALIO_ARCH "arm64"
#else
#define SCALIO_ARCH "x64"
#endif

int main(int argc, char *argv[])
{
    char self[PATH_MAX];
    uint32_t size = sizeof(self);

    if (_NSGetExecutablePath(self, &size) != 0) {
        fprintf(stderr, "Scalio: başlatıcı yolu okunamadı\n");
        return 1;
    }

    char resolved[PATH_MAX];

    if (realpath(self, resolved) == NULL) {
        fprintf(stderr, "Scalio: başlatıcı yolu çözülemedi\n");
        return 1;
    }

    char target[PATH_MAX];
    snprintf(target, sizeof(target), "%s/%s/Scalio", dirname(resolved), SCALIO_ARCH);

    argv[0] = target;
    execv(target, argv);

    perror("Scalio: uygulama başlatılamadı");
    return 1;
}
