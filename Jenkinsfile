pipeline {
    agent any
    stages {
        stage('Restore') {
            steps {
                sh 'docker build --target restore .'
            }
        }
        stage('Build') {
            steps {
                sh 'docker build --target build .'
            }
        }
        stage('Test') {
            steps {
                sh 'docker build --target test .'
            }
        }
        stage('Delivery docker images') {
            when { 
                anyOf { 
                    branch 'main'
                    branch 'INT'
                    buildingTag()
                }
            }
            environment {
                DOCKER_NAMESPACE = 'dopplerdock'
                FULL_VERSION = "${BRANCH_NAME}-commit-${GIT_COMMIT}"
            }
            steps {
                withDockerRegistry([ credentialsId: "dockerhub_${DOCKER_NAMESPACE}", url: ""]) {
                    sh 'env VERSION=${BRANCH_NAME} docker-compose build'
                    sh 'env VERSION=${BRANCH_NAME} docker-compose push'
                    sh 'docker-compose build'
                    sh 'docker-compose push'
                }
            }
        }

        stage('Delivery semver docker images') {
            when {
                expression {
                    return isVersionTag(readCurrentTag())
                }
            }
            environment {
                DOCKER_NAMESPACE = 'dopplerdock'
                SEMVER_MAYOR = "${env.BRANCH_NAME.tokenize('.')[0]}"
                SEMVER_MAYOR_MINOR = "${SEMVER_MAYOR}.${env.BRANCH_NAME.tokenize('.')[1]}"
                SEMVER_MAYOR_MINOR_PATCH = "${SEMVER_MAYOR_MINOR}.${env.BRANCH_NAME.tokenize('.')[2]}"
                FULL_VERSION = "${BRANCH_NAME}-commit-${GIT_COMMIT}"
            }
            steps {
                withDockerRegistry([ credentialsId: "dockerhub_${DOCKER_NAMESPACE}", url: ""]) {
                    sh 'env VERSION=${SEMVER_MAYOR} docker-compose build'
                    sh 'env VERSION=${SEMVER_MAYOR} docker-compose push'
                    sh 'env VERSION=${SEMVER_MAYOR_MINOR} docker-compose build'
                    sh 'env VERSION=${SEMVER_MAYOR_MINOR} docker-compose push'
                    sh 'env VERSION=${SEMVER_MAYOR_MINOR_PATCH} docker-compose build'
                    sh 'env VERSION=${SEMVER_MAYOR_MINOR_PATCH} docker-compose push'
                }
            }
        }
    }
    post { 
        cleanup {
            cleanWs()
            dir("${env.WORKSPACE}@tmp") {
                deleteDir()
            }
        }
    }
}

def boolean isVersionTag(String tag) {
    echo "checking version tag $tag"

    if (tag == null) {
        return false
    }

    // use your preferred pattern
    def tagMatcher = tag =~ /v\d+\.\d+\.\d+/

    return tagMatcher.matches()
}

// https://stackoverflow.com/questions/56030364/buildingtag-always-returns-false
// workaround https://issues.jenkins-ci.org/browse/JENKINS-55987
// TODO: read this value from Jenkins provided metadata
def String readCurrentTag() {
    return sh(returnStdout: true, script: 'echo ${TAG_NAME}').trim()
}
