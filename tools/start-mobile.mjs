import { spawn, spawnSync } from 'node:child_process';

const [platform, mode, ...flutterArgs] = process.argv.slice(2);

function fail(message) {
  process.stderr.write(`${message}\n`);
  process.exit(1);
}

if (!['android', 'ios'].includes(platform) || !['debug', 'release'].includes(mode)) {
  fail('Usage: start-mobile.mjs <android|ios> <debug|release> [Flutter arguments]');
}

if (platform === 'ios' && process.platform !== 'darwin') {
  fail(`iOS run targets require macOS with Xcode and Flutter configured. Current host: ${process.platform}.`);
}

let requestedDeviceId;
const passthroughArgs = [];

for (let index = 0; index < flutterArgs.length; index += 1) {
  const argument = flutterArgs[index];
  let candidate;

  if (argument.startsWith('--device-id=')) {
    candidate = argument.slice('--device-id='.length);
  } else if (argument === '--device-id' || argument === '-d') {
    candidate = flutterArgs[index + 1];
    index += 1;
  } else {
    passthroughArgs.push(argument);
    continue;
  }

  if (!candidate) {
    fail(`${argument} requires a device ID.`);
  }

  if (requestedDeviceId && requestedDeviceId !== candidate) {
    fail('Specify only one device ID.');
  }

  requestedDeviceId = candidate;
}

const deviceResult = spawnSync('flutter', ['devices', '--machine'], { encoding: 'utf8' });

if (deviceResult.error) {
  fail(`Unable to run Flutter: ${deviceResult.error.message}`);
}

if (deviceResult.status !== 0) {
  if (deviceResult.stdout?.trim()) {
    process.stderr.write(`${deviceResult.stdout.trim()}\n`);
  }
  if (deviceResult.stderr?.trim()) {
    process.stderr.write(`${deviceResult.stderr.trim()}\n`);
  }
  fail('Unable to list Flutter devices.');
}

let devices;

try {
  devices = JSON.parse(deviceResult.stdout);
} catch {
  fail('Flutter returned an invalid device list. Run `flutter devices` to inspect the environment.');
}

if (!Array.isArray(devices)) {
  fail('Flutter returned an invalid device list. Run `flutter devices` to inspect the environment.');
}

const eligibleDevices = devices.filter((device) => {
  const targetPlatform = String(device.targetPlatform ?? '');
  return platform === 'android' ? targetPlatform.startsWith('android-') : targetPlatform === 'ios';
});

const platformName = platform === 'android' ? 'Android' : 'iOS';
const formatDevices = (items) => items
  .map((device) => `  - ${device.name} (${device.id}) [${device.targetPlatform}]${device.emulator ? ' emulator/simulator' : ''}`)
  .join('\n');

let selectedDevice;

if (requestedDeviceId) {
  selectedDevice = eligibleDevices.find((device) => device.id === requestedDeviceId);

  if (!selectedDevice) {
    const available = eligibleDevices.length > 0
      ? `\nEligible ${platformName} devices:\n${formatDevices(eligibleDevices)}`
      : ` No eligible ${platformName} devices are connected.`;
    fail(`Device "${requestedDeviceId}" is not an eligible ${platformName} device.${available}`);
  }
} else if (eligibleDevices.length === 1) {
  [selectedDevice] = eligibleDevices;
} else if (eligibleDevices.length === 0) {
  fail(`No eligible ${platformName} device is connected. Connect one and run \`flutter devices\`. This command does not start an emulator or simulator.`);
} else {
  fail(`Multiple ${platformName} devices are available:\n${formatDevices(eligibleDevices)}\nRun again with --device-id=<ID> or -d <ID>.`);
}

if (mode === 'release' && selectedDevice.emulator) {
  fail(`Release mode requires a physical device. "${selectedDevice.name}" (${selectedDevice.id}) is an emulator or simulator.`);
}

const child = spawn('flutter', ['run', `--${mode}`, '-d', selectedDevice.id, ...passthroughArgs], {
  stdio: 'inherit',
});

let launchError;
let forwardedSignal;
const signalHandlers = new Map();

for (const signal of ['SIGINT', 'SIGTERM', 'SIGHUP']) {
  const handler = () => {
    forwardedSignal ??= signal;
    child.kill(signal);
  };
  signalHandlers.set(signal, handler);
  process.on(signal, handler);
}

function removeSignalHandlers() {
  for (const [signal, handler] of signalHandlers) {
    process.off(signal, handler);
  }
}

child.once('error', (error) => {
  launchError = error;
});

child.once('close', (code, signal) => {
  removeSignalHandlers();

  if (launchError) {
    process.stderr.write(`Unable to start Flutter: ${launchError.message}\n`);
    process.exitCode = 1;
    return;
  }

  const exitSignal = forwardedSignal ?? signal;
  if (exitSignal) {
    process.kill(process.pid, exitSignal);
    return;
  }

  process.exitCode = code ?? 1;
});
