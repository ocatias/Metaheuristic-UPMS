import sys
import subprocess
import re

if __name__ == '__main__':
    # instance, instance_specific, cutoff, runlength = sys.argv[1:5]
    seed = sys.argv[5]

    # 7 alpha, 9 ns,  11 pB, 13 pI, 15 pM, 17 pS, 19 pT, 21 tMax, 23 tMin

    runtime = 5

    p = subprocess.Popen(["mono", "/home/fabian/Documents/Informatik/CO/CO1/SASolver/bin/Release/netcoreapp3.1/publish/SASolver.dll", sys.argv[1],
    str(runtime), 'false', sys.argv[21], sys.argv[23], str(int(float(sys.argv[9]))), sys.argv[13], sys.argv[17], sys.argv[11],
    sys.argv[19], sys.argv[15], '0 ', sys.argv[7]], stdout=subprocess.PIPE)
    result = p.communicate()[0]
    resultParsed = re.sub('\D', '', str(result))
    print(resultParsed)

    print('Result for SMAC: SUCCESS, -1, -1, %s, %s' % (resultParsed, seed))
