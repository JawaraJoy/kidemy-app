---
name: kidemy-challenge
description: Gunakan di repo Unity kidemy-app untuk mengecek koneksi Unity MCP, hierarchy scene aktif dan Console, atau membuat dan merevisi challenge dari bank soal dan referensi layout dengan struktur folder dan UI bawaan Kidemy.
---

# Challenge Kidemy

Gunakan path relatif terhadap root `kidemy-app`. Untuk tugas inspeksi, cukup laporkan keadaan Editor. Untuk pembuatan atau revisi, kerjakan pada scene dan aset milik challenge yang diminta.

## Periksa Editor

Cocokkan instance dari `mcpforunity://instances` dengan path proyek pada `mcpforunity://project/info`. Baca `mcpforunity://editor/state` untuk mengetahui path scene aktif, mode Edit atau Play, dan status kompilasi. Untuk menjelaskan scene, ambil hierarchy lewat `manage_scene` dengan aksi `get_hierarchy` dan jelaskan fungsi root serta anak penting berdasarkan komponennya. Baca Console lewat `read_console` dengan mempertahankan log yang ada. Laporkan error atau exception dan warning secara terpisah, termasuk pesan serta lokasinya bila tersedia. Jika koneksi terputus, sebutkan bagian yang belum bisa diverifikasi di Editor.

## Buat challenge dari bank soal

1. Ambil island, kelompok usia, mekanik, bahasa instruksi, urutan soal, pilihan, dan kunci dari brief. Gunakan gambar yang diberikan sebagai referensi layout sesuai permintaan pengguna. Contoh angka, kata, atau gambar dalam mockup hanya menjadi isi soal jika brief menetapkannya.
2. Untuk pilihan ganda, periksa saudara terdekat di `Assets/_dev2/Quest/Multiple Choice/`. `Island_1_HowManyFrog` memberi contoh wiring dasar, `Island_2_RhymeTime` untuk pilihan bergambar, dan `Island_1_TensAndOnes` untuk pilihan angka. Untuk mekanik lain, cari contoh dengan interaksi yang sama.
3. Ikuti pola berikut untuk challenge pilihan ganda baru. Gunakan nama challenge dalam PascalCase dan nomor soal tiga digit. Buat `Dialogue`, `Images`, `Audio`, `Prefabs`, atau `Scripts` sesuai kebutuhan konten. Saat merevisi, gunakan folder, nama, dan GUID yang sudah ada.

```text
Assets/_dev2/Quest/Multiple Choice/Island_<nomor>_<Nama>/
  OtherDev_<Nama>_A_001.unity
  SceneConfig_<Nama>_A_001.asset
  Configs/
    Challenge_<Nama>_A.asset
    SO_<Nama>_A_001.asset
    Dialogue/
  Images/
  Audio/
  Prefabs/
  Scripts/
```

Hubungkan `SceneConfig.m_SceneAsset` ke scene baru dan `GameManager.m_ChallengeConfig` ke `ChallengeConfig` lokal. Isi `m_SceneConfig`, `m_QuestLayoutPrefabs`, `m_ChoicePrefab` dan `m_QuestConfigs` sesuai urutan bank soal. Gunakan satu `SO_QuestMultipleChoice` per soal. Pakai latar dan karakter island dari contoh yang sesuai. Dialog atau klip instruksi yang sama bisa dipakai ulang.

## Pertahankan tampilan bawaan proyek

Acuan panel pertanyaan dan fullscreen adalah `Assets/_dev2/Quest/Prefabs/Layouts/Stage_OtherDev.prefab`. Warisi sprite, material font, gaya, ukuran otomatis, dan fungsi tombol dari prefab. Atur posisi pada instance scene dan buat isi permainan di prefab lokal. Jika perlu memulihkan tampilan, lepaskan override visual pada instance sambil mempertahankan teks soal dan posisi yang diminta.

| Elemen | Aset acuan |
| --- | --- |
| Panel pertanyaan | `SP_Container` putih berbayang, ornamen bawaan, teks `Nunito-BoldItalic SDF` abu-abu gelap |
| Ulang audio | Tombol bulat `Circles/Yellow.png`, ikon putih `Sound On`, aksi `GameManager.PlayQuestionVoice` |
| Fullscreen | Tombol bulat `Circles/Red.png`, ikon `SP_Fullscreen` dan `SP_FullscreenExit` |
| Teks pilihan dan angka | `Nunito-Bold SDF` beserta material dari `Assets/_dev2/Quest/Prefabs/Layouts/ChoicesItem_Text.prefab` |

Gunakan prefab bawaan `Assets/_dev2/Quest/Prefabs/Reaction Panel.prefab` beserta animasinya. Reaction Panel harus memenuhi seluruh Game View sejak muncul. Jika `AspectRatioFitter` atau ukuran tetap area kartu membatasi `GameManager.m_PanelContainer`, arahkan field tersebut pada instance scene ke root Canvas `Stage_OtherDev/UI`, seperti pada First Sound Pop.

Perlakukan badge seperti “Option 1” atau “Option 3” dan coretan penanda sebagai anotasi desain. Saat menghapus badge contoh, lepaskan juga binding tampilannya bila terpasang. Hitung ulang posisi grup yang sebelumnya menyisakan ruang untuk badge. Pada layout terpusat seperti Tens and Ones, cocokkan pusat panel dan teks instruksi dengan pembatas papan serta pusat baris pilihan. Beri ruang di kiri dan kanan untuk karakter dan speaker.

## Verifikasi di Unity

Buka ulang scene untuk memeriksa referensi tersimpan. Di Play Mode, cocokkan stimulus, jumlah pilihan dan kunci tiap soal dengan bank soal. Untuk blok puluhan, periksa sepuluh ruas per batang dan kolom satuan yang terlihat kosong saat nilainya nol. Uji klik atau tap melalui tombol, audio replay bila digunakan, umpan balik benar dan salah, perpindahan soal, dan skor akhir. Jika `GameManager.SendResultInternal` memakai API, isolasi pengiriman pada instance Play Mode agar skor tes tetap lokal.

Lihat Game View pada rasio target, termasuk 16:9 dan 16:10 untuk challenge landscape. Pada tiap rasio, pilih jawaban yang memunculkan Reaction Panel. Pastikan keempat tepi `Reaction Panel/Content` berimpit dengan tepi Game View sejak panel terlihat hingga animasi selesai, lalu uji tombol lanjut.

Untuk revisi visual, periksa perataan, bagian yang terpotong, serta area klik. Laporkan scene yang dikerjakan, hasil uji yang diamati, dan status Console. Sertakan tangkapan Game View saat hasilnya perlu dibandingkan dengan referensi layout.
