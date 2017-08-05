#include "pch.h"
#include "Class1.h"

using namespace DHT11;
using namespace Platform;

using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::UI::Xaml;
using namespace Windows::UI::Xaml::Controls;
using namespace Windows::UI::Xaml::Controls::Primitives;
using namespace Windows::UI::Xaml::Data;
using namespace Windows::UI::Xaml::Input;
using namespace Windows::UI::Xaml::Media;
using namespace Windows::UI::Xaml::Navigation;
using namespace Windows::UI::Core;
using namespace Windows::System::Threading;
using namespace Windows::Devices::Gpio;

void DHT11::DhtSensor::Init(GpioPin^ Pin)
{
	// Use InputPullUp if supported, otherwise fall back to Input (floating)
	this->inputDriveMode =
		Pin->IsDriveModeSupported(GpioPinDriveMode::InputPullUp) ?
		GpioPinDriveMode::InputPullUp : GpioPinDriveMode::Input;

	Pin->SetDriveMode(this->inputDriveMode);
	this->pin = Pin;
}

_Use_decl_annotations_
HRESULT DHT11::DhtSensor::Sample(DHT11::DhtSensorReading& Reading)
{
	Reading = DhtSensorReading();

	LARGE_INTEGER qpf;
	QueryPerformanceFrequency(&qpf);

	// This is the threshold used to determine whether a bit is a '0' or a '1'.
	// A '0' has a pulse time of 76 microseconds, while a '1' has a
	// pulse time of 120 microseconds. 110 is chosen as a reasonable threshold.
	// We convert the value to QPF units for later use.
	const unsigned int oneThreshold = static_cast<unsigned int>(
		110LL * qpf.QuadPart / 1000000LL);

	// Latch low value onto pin
	this->pin->Write(GpioPinValue::Low);

	// Set pin as output
	this->pin->SetDriveMode(GpioPinDriveMode::Output);

	// Wait for at least 18 ms
	Sleep(SAMPLE_HOLD_LOW_MILLIS);

	// Set pin back to input
	this->pin->SetDriveMode(this->inputDriveMode);

	GpioPinValue previousValue = this->pin->Read();

	// catch the first rising edge
	const ULONG initialRisingEdgeTimeoutMillis = 1;
	ULONGLONG endTickCount = GetTickCount64() + initialRisingEdgeTimeoutMillis;
	for (;;) {
		if (GetTickCount64() > endTickCount) {
			return HRESULT_FROM_WIN32(ERROR_TIMEOUT);
		}

		GpioPinValue value = this->pin->Read();
		if (value != previousValue) {
			// rising edgue?
			if (value == GpioPinValue::High) {
				break;
			}
			previousValue = value;
		}
	}

	LARGE_INTEGER prevTime = { 0 };

	const ULONG sampleTimeoutMillis = 10;
	endTickCount = GetTickCount64() + sampleTimeoutMillis;

	// capture every falling edge until all bits are received or
	// timeout occurs
	for (unsigned int i = 0; i < (Reading.bits.size() + 1);) {
		if (GetTickCount64() > endTickCount) {
			return HRESULT_FROM_WIN32(ERROR_TIMEOUT);
		}

		GpioPinValue value = this->pin->Read();
		if ((previousValue == GpioPinValue::High) && (value == GpioPinValue::Low)) {
			// A falling edge was detected
			LARGE_INTEGER now;
			QueryPerformanceCounter(&now);

			if (i != 0) {
				unsigned int difference = static_cast<unsigned int>(
					now.QuadPart - prevTime.QuadPart);
				Reading.bits[Reading.bits.size() - i] =
					difference > oneThreshold;
			}

			prevTime = now;
			++i;
		}

		previousValue = value;
	}

	if (!Reading.IsValid()) {
		// checksum mismatch
		return HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
	}

	return S_OK;
}



void DHT11::MainPage::Page_Loaded()
{
	GpioController^ controller = GpioController::GetDefault();
	if (!controller) {
		
		return;
	}

	GpioPin^ pin;
	try {
		pin = controller->OpenPin(DHT_PIN_NUMBER);
	}
	catch (Exception^ ex) {
		
		return;
	}

	this->dhtSensor.Init(pin);
	

	TimeSpan period = { 2 * 10000000LL };
	this->timer = ThreadPoolTimer::CreatePeriodicTimer(
		ref new TimerElapsedHandler(this, &MainPage::timerElapsed),
		period);
}

void DHT11::MainPage::timerElapsed(Windows::System::Threading::ThreadPoolTimer^ Timer)
{
	HRESULT sensorHr;
	DhtSensorReading reading;

	int retryCount = 0;
	do {
		sensorHr = this->dhtSensor.Sample(reading);
	} while (FAILED(sensorHr) && (++retryCount < 20));
	humidity = reading.Humidity();
	temperature = reading.Temperature();
	

	
}

double MainPage::Humidity()
{
	
	return humidity;
}

double MainPage::Temperature()
{
	
	return temperature;
}

