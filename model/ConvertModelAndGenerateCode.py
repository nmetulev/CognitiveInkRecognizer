'Convert CoreML model to ONNX-compliant model and generate C# proxy'

import os
import winmltools
import coremltools

MODEL_DIR_SOURCE = '.'
MODEL_DIR_CONVERTED = '.\\Models\\Converted'
MODEL_DIR_GENERATED = '.\\Models\\GeneratedProxy'

MODEL_NAMESPACE = 'MyModels'

MLGEN_PATH = 'C:\\Tools\\WinML\\mlgen.exe'

FILES = os.listdir(MODEL_DIR_SOURCE)
for filename in FILES:
    if filename.endswith('.mlmodel'):
        print('processing ' + MODEL_DIR_SOURCE + '\\' + filename)
        filename_only = filename.rsplit('.', 1)[0]
        onnx_filename = filename_only + '.onnx'
        json_filename = filename_only + '.json'
        csharp_filename = filename_only + '.cs'
        cml = coremltools.utils.load_spec(MODEL_DIR_SOURCE + '\\' + filename)
        print('CoreML model loaded: ' + filename + ', starting Windows ML conversion...')
        try:
            winml = winmltools.convert.convert_coreml(cml, filename_only)
            print('Writing out onnx model: ' + MODEL_DIR_CONVERTED + '\\' + onnx_filename)
            with open(MODEL_DIR_CONVERTED + '\\' + onnx_filename, 'wb') as f:
                f.write(winml.SerializeToString())
            print('Writing out json model: ' + MODEL_DIR_CONVERTED + '\\' + json_filename)
            with open(MODEL_DIR_CONVERTED + '\\' + json_filename, 'w') as f:
                f.write(str(winml))
            # print('Generating C# proxy stub: ' + MODEL_DIR_GENERATED + '\\' + csharp_filename)
            # command_line = MLGEN_PATH + ' -i ' + MODEL_DIR_CONVERTED + '\\' + onnx_filename + ' -l CS -o ' + MODEL_DIR_GENERATED + '\\' + csharp_filename + ' -n ' + MODEL_NAMESPACE
            #  print('Executing command line: ' + command_line)
            # os.system(command_line)
        except Exception as exception:
            print('FAILED TO CONVERT ' + filename)