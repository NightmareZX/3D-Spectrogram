var context, analyser, dataArray, bandpass, mix, filterGain;
var audio, source;
async function loadAndPlayAudio(src) {
    // Create audio context and analyser
    context = new (window.AudioContext || window.webkitAudioContext)();
    analyser = context.createAnalyser();
    analyser.fftSize = 2048; // Adjust fftSize as needed
    analyser.smoothingTimeConstant = 0;
    dataArray = new Uint8Array(analyser.frequencyBinCount);
    bandpass = context.createBiquadFilter();
    bandpass.Q.value = 10;
    bandpass.type = 'bandpass';

    mix = context.createGain();

    filterGain = context.createGain();
    filterGain.gain.value = 1;

    mix.connect(analyser);
    analyser.connect(filterGain);
    filterGain.connect(context.destination);

    // Load and play the audio file
    audio = new Audio(src)
    source = context.createMediaElementSource(audio);
    source.connect(analyser);
    analyser.connect(context.destination);
    //audio.volume = 1
    audio.play();
}

function getFrequencyData() {
    analyser.getByteFrequencyData(dataArray);
    // Send frequency data to C#
    window.chrome.webview.postMessage(Array.from(dataArray));
}