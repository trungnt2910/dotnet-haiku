#!/bin/bash -e

DOTNET_PATH="dotnet"

if [ -z "$DOTNET_ROOT" ]; then
    DOTNET_PATH=$(command -v dotnet)
else
    DOTNET_PATH="$DOTNET_ROOT/dotnet"
fi

if [ -z "$DOTNET_PATH" ]; then
    echo "Could not find dotnet. Install dotnet and try again."
    exit 1
fi

echo "Using dotnet from $DOTNET_PATH"

DOTNET_VERSION=$(eval "$DOTNET_PATH --version")

echo "Detected .NET SDK version $DOTNET_VERSION"

set +e

prereleaseStart=$(($(expr index "$DOTNET_VERSION" "-") - 1))

if [ "$prereleaseStart" -gt 0 ]; then
    firstDot=$(expr index "${DOTNET_VERSION:$(($prereleaseStart + 1)):${#DOTNET_VERSION}}" ".")
    firstDot=$(($firstDot + $prereleaseStart))
    secondDot=$(expr index "${DOTNET_VERSION:$((firstDot + 1)):${#DOTNET_VERSION}}" ".")

    if [ "$secondDot" -eq 0 ]; then
        secondDot="${#DOTNET_VERSION}"
    else
        secondDot=$(($secondDot + $firstDot))
    fi

    prereleaseKind="${DOTNET_VERSION:$((prereleaseStart + 1)):$((firstDot - prereleaseStart - 1))}"

    if [ "$prereleaseKind" = "servicing" ]; then
        DOTNET_FEATURE_BAND="${DOTNET_VERSION:0:$((prereleaseStart - 2))}00"
    else
        DOTNET_FEATURE_BAND="${DOTNET_VERSION:0:$secondDot}"
    fi
else
    DOTNET_FEATURE_BAND="${DOTNET_VERSION:0:$((${#DOTNET_VERSION} - 2))}00"
fi

set -e

echo "Detected .NET SDK feature band $DOTNET_FEATURE_BAND"

GITHUB_SERVER_URL=${GITHUB_SERVER_URL:-"https://github.com"}
GITHUB_API_URL=${GITHUB_API_URL:-"https://api.github.com"}
GITHUB_REPOSITORY=${GITHUB_REPOSITORY:-"trungnt2910/dotnet-haiku"}

manifestName="trungnt2910.net.sdk.haiku"
manifestPackageName="$manifestName.manifest-$DOTNET_FEATURE_BAND"

manifestPackageVersion=""
releaseUrl="$GITHUB_SERVER_URL/$GITHUB_REPOSITORY/releases/tag/"
pageNum=1
while [ -z "$manifestPackageVersion" ];
do
    json=$(curl -s $GITHUB_API_URL/repos/$GITHUB_REPOSITORY/releases?page=$pageNum)
    pageNum=$((pageNum + 1))
    if [ $(echo $json | jq length) -eq 0 ]; then
        # This means that we've passed the end and reached an empty array
        break
    fi
    if [ $(echo $json | jq 'objects // {} | has("message")') == "true" ]; then
        # API has return an error object
        echo "Unable to fetch releases from GitHub API"
        exit 2
    fi
    # Store array of revisions
    revisions=($(echo $json | jq -e -r ".[] | .html_url | select(true)[${#releaseUrl}:]")) \
        || continue
    manifestPackageVersion=${revisions[0]}
done

echo "Latest manifest package version is $manifestPackageVersion"

manifestPackageUrl="$GITHUB_SERVER_URL/$GITHUB_REPOSITORY/releases/download/$manifestPackageVersion/$manifestPackageName.$manifestPackageVersion.nupkg"

echo "Downloading manifest package from $manifestPackageUrl"

manifestInstallDir="$(dirname "$DOTNET_PATH")/sdk-manifests/$DOTNET_FEATURE_BAND/$manifestName"

tmpFilePath="$(mktemp)"
curl -sL $manifestPackageUrl -u $GITHUB_USERNAME:$GITHUB_TOKEN -o $tmpFilePath
unzip -o -j $tmpFilePath 'data/*.*' -d $manifestInstallDir
rm $tmpFilePath

echo "Installed manifest package to $manifestInstallDir"
echo "Run '$DOTNET_PATH workload install haiku' to install the Haiku workload."
