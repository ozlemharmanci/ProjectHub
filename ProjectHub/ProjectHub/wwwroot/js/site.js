// Proje Hub için genel JavaScript fonksiyonları
console.log('ProjectHub JavaScript yüklendi');

// Form onayları için
function confirmAction(message) {
    return confirm(message || 'Bu işlemi yapmak istediğinize emin misiniz?');
}

// Dosya yükleme validasyonu
function validateFile(input, maxSizeMB = 100) {
    if (input.files && input.files[0]) {
        const file = input.files[0];
        const maxSize = maxSizeMB * 1024 * 1024;

        if (!file.name.toLowerCase().endsWith('.zip')) {
            alert('Sadece ZIP dosyaları kabul edilir.');
            input.value = '';
            return false;
        }

        if (file.size > maxSize) {
            alert(`Dosya boyutu ${maxSizeMB}MB'dan büyük olamaz.`);
            input.value = '';
            return false;
        }
    }
    return true;
}