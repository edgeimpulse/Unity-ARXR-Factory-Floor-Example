# Builds the Edge Impulse FOMO inference wrapper as a shared library for Unity.
#
#   macOS (Editor / simulator):  ./build_macos.sh
#   Android arm64 (Quest 3/3S):  ./build_android.sh
#
# Both scripts invoke this Makefile with the right compiler and SHARED flag.

CC  ?= gcc
CXX ?= g++

NAME       ?= libei_fomo.so
BUILD_PATH ?= ./build
SHARED     ?= -shared

# ---- flags ---------------------------------------------------------------
CFLAGS   += -I.
CFLAGS   += -Wall -g -Wno-strict-aliasing
CFLAGS   += -Os -DNDEBUG -fPIC
CFLAGS   += -DEI_CLASSIFIER_ENABLE_DETECTION_POSTPROCESS_OP
CXXFLAGS += -std=c++17
LDFLAGS  += -lm

# ---- our wrapper ---------------------------------------------------------
CXXSOURCES = ei_fomo.cpp

# ---- Edge Impulse DSP (CMSIS) --------------------------------------------
CSOURCES += $(wildcard edge-impulse-sdk/CMSIS/DSP/Source/TransformFunctions/*.c) \
            $(wildcard edge-impulse-sdk/CMSIS/DSP/Source/CommonTables/*.c) \
            $(wildcard edge-impulse-sdk/CMSIS/DSP/Source/BasicMathFunctions/*.c) \
            $(wildcard edge-impulse-sdk/CMSIS/DSP/Source/ComplexMathFunctions/*.c) \
            $(wildcard edge-impulse-sdk/CMSIS/DSP/Source/FastMathFunctions/*.c) \
            $(wildcard edge-impulse-sdk/CMSIS/DSP/Source/SupportFunctions/*.c) \
            $(wildcard edge-impulse-sdk/CMSIS/DSP/Source/MatrixFunctions/*.c) \
            $(wildcard edge-impulse-sdk/CMSIS/DSP/Source/StatisticsFunctions/*.c)

# ---- Edge Impulse SDK + model --------------------------------------------
CXXSOURCES += $(wildcard tflite-model/*.cpp) \
              $(wildcard edge-impulse-sdk/classifier/*.cpp) \
              $(wildcard edge-impulse-sdk/dsp/kissfft/*.cpp) \
              $(wildcard edge-impulse-sdk/dsp/image/*.cpp) \
              $(wildcard edge-impulse-sdk/dsp/spectral/*.cpp) \
              $(wildcard edge-impulse-sdk/dsp/speechpy/*.cpp) \
              $(wildcard edge-impulse-sdk/dsp/dct/*.cpp) \
              $(wildcard edge-impulse-sdk/dsp/memory.cpp) \
              $(wildcard edge-impulse-sdk/porting/posix/*.cpp) \
              $(wildcard edge-impulse-sdk/porting/mingw32/*.cpp)

# ---- TensorFlow Lite for Microcontrollers --------------------------------
CFLAGS   += -DTF_LITE_DISABLE_X86_NEON=1
CSOURCES += edge-impulse-sdk/tensorflow/lite/c/common.c
CCSOURCES += $(wildcard edge-impulse-sdk/tensorflow/lite/kernels/*.cc) \
             $(wildcard edge-impulse-sdk/tensorflow/lite/kernels/internal/*.cc) \
             $(wildcard edge-impulse-sdk/tensorflow/lite/micro/kernels/*.cc) \
             $(wildcard edge-impulse-sdk/tensorflow/lite/micro/*.cc) \
             $(wildcard edge-impulse-sdk/tensorflow/lite/micro/memory_planner/*.cc) \
             $(wildcard edge-impulse-sdk/tensorflow/lite/core/api/*.cc)

COBJECTS   := $(patsubst %.c,%.o,$(CSOURCES))
CXXOBJECTS := $(patsubst %.cpp,%.o,$(CXXSOURCES))
CCOBJECTS  := $(patsubst %.cc,%.o,$(CCSOURCES))

.PHONY: all
all: lib

%.o: %.c
	$(CC) $(CFLAGS) -c $^ -o $@
%.o: %.cc
	$(CXX) $(CFLAGS) $(CXXFLAGS) -c $^ -o $@
%.o: %.cpp
	$(CXX) $(CFLAGS) $(CXXFLAGS) -c $^ -o $@

.PHONY: lib
lib: $(COBJECTS) $(CXXOBJECTS) $(CCOBJECTS)
	mkdir -p $(BUILD_PATH)
	$(CXX) $(COBJECTS) $(CXXOBJECTS) $(CCOBJECTS) $(SHARED) -o $(BUILD_PATH)/$(NAME) $(LDFLAGS)
	@echo "Built $(BUILD_PATH)/$(NAME)"

.PHONY: clean
clean:
	find . -name '*.o' -delete
	rm -rf $(BUILD_PATH)
