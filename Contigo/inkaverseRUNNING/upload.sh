#!/bin/bash

# 1) Upload everything EXCEPT *.br, let AWS CLI auto-detect Content-Type
aws s3 cp . s3://inkaverse-bucket/ \
  --recursive \
  --exclude "*.br" \
  --acl public-read

# 2) Now upload all *.br files with correct system-defined Content-Encoding
aws s3 cp . s3://inkaverse-bucket/ \
  --recursive \
  --exclude "*" \
  --include "*.br" \
  --content-encoding br \
  --acl public-read

echo "Upload complete to inkaverse-bucket"

