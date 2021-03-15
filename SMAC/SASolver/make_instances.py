import sys
import glob, os

if __name__ == '__main__':
    pathToInstances = "/home/fabian/Documents/Informatik/CO/Data/instances/training"
    pathToMakeInstanceFile = "/home/fabian/SMAC/SASolver/instances.txt"
    os.chdir(pathToInstances)

    finalFile = ""
    for filename in glob.glob("*.max"):
        finalFile += pathToInstances + "/" + filename + "\n"

    f = open(pathToMakeInstanceFile, "a")
    f.truncate(0) #clear file
    f.write(finalFile)
    f.close()
