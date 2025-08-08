// Test script for Online Users system
class OnlineUsersTest {
    constructor() {
        this.baseUrl = window.location.origin;
        this.results = [];
    }

    log(message, type = 'info') {
        const timestamp = new Date().toISOString();
        const logEntry = { timestamp, message, type };
        this.results.push(logEntry);
        console.log(`[${timestamp}] ${type.toUpperCase()}: ${message}`);
    }

    async testAPI(endpoint, method = 'GET', body = null) {
        try {
            const options = {
                method: method,
                headers: {
                    'Content-Type': 'application/json'
                }
            };

            if (body) {
                options.body = JSON.stringify(body);
            }

            const response = await fetch(`${this.baseUrl}${endpoint}`, options);
            const responseText = await response.text();
            
            let responseData;
            try {
                responseData = JSON.parse(responseText);
            } catch {
                responseData = responseText;
            }

            return {
                success: response.ok,
                status: response.status,
                data: responseData,
                headers: Object.fromEntries(response.headers.entries())
            };
        } catch (error) {
            return {
                success: false,
                error: error.message
            };
        }
    }

    async runAllTests() {
        this.log('Starting Online Users System Tests', 'info');
        
        // Test 1: Check API endpoints availability
        await this.testEndpointAvailability();
        
        // Test 2: Test database connection
        await this.testDatabaseConnection();
        
        // Test 3: Test service methods
        await this.testServiceMethods();
        
        // Test 4: Test JavaScript integration
        await this.testJavaScriptIntegration();
        
        this.log('All tests completed', 'info');
        return this.results;
    }

    async testEndpointAvailability() {
        this.log('Testing API endpoint availability...', 'info');
        
        const endpoints = [
            '/api/OnlineUsers/count',
            '/api/OnlineUsers/list',
            '/Debug/Online/TestDatabase',
            '/Debug/Online/TestService'
        ];

        for (const endpoint of endpoints) {
            const result = await this.testAPI(endpoint);
            if (result.success) {
                this.log(`✓ ${endpoint} - Status: ${result.status}`, 'success');
            } else {
                this.log(`✗ ${endpoint} - Error: ${result.error || result.status}`, 'error');
            }
        }
    }

    async testDatabaseConnection() {
        this.log('Testing database connection...', 'info');
        
        const result = await this.testAPI('/Debug/Online/TestDatabase');
        if (result.success) {
            const data = result.data;
            this.log(`✓ Database connection OK - Found ${data.TotalConnections} connections`, 'success');
            
            if (data.Connections && data.Connections.length > 0) {
                this.log(`Sample connection: ${JSON.stringify(data.Connections[0])}`, 'info');
            }
        } else {
            this.log(`✗ Database connection failed: ${result.error || JSON.stringify(result.data)}`, 'error');
        }
    }

    async testServiceMethods() {
        this.log('Testing service methods...', 'info');
        
        const result = await this.testAPI('/Debug/Online/TestService');
        if (result.success) {
            const data = result.data;
            this.log(`✓ Service methods OK - ${data.OnlineCount} users online`, 'success');
            
            if (data.OnlineUsers && data.OnlineUsers.length > 0) {
                this.log(`Sample user: ${JSON.stringify(data.OnlineUsers[0])}`, 'info');
            }
        } else {
            this.log(`✗ Service methods failed: ${result.error || JSON.stringify(result.data)}`, 'error');
        }
    }

    async testJavaScriptIntegration() {
        this.log('Testing JavaScript integration...', 'info');
        
        // Check if OnlineUserManager exists
        if (typeof window.onlineUserManager !== 'undefined') {
            this.log('✓ OnlineUserManager is loaded', 'success');
            
            // Check if user is authenticated
            const isAuth = window.onlineUserManager.isAuthenticated;
            this.log(`User authentication status: ${isAuth}`, 'info');
            
            // Check connection status
            const hasConnection = window.onlineUserManager.connectionId !== null;
            this.log(`Connection status: ${hasConnection ? 'Connected' : 'Not connected'}`, hasConnection ? 'success' : 'warning');
            
        } else {
            this.log('✗ OnlineUserManager is not loaded', 'error');
        }
        
        // Test footer elements
        const onlineCountElement = document.getElementById('online-number');
        const onlineUsersDropdown = document.getElementById('onlineUsersDropdown');
        
        if (onlineCountElement) {
            this.log(`✓ Online count element found - Current value: ${onlineCountElement.textContent}`, 'success');
        } else {
            this.log('✗ Online count element not found', 'error');
        }
        
        if (onlineUsersDropdown) {
            this.log('✓ Online users dropdown found', 'success');
        } else {
            this.log('✗ Online users dropdown not found', 'error');
        }
    }

    generateReport() {
        const successCount = this.results.filter(r => r.type === 'success').length;
        const errorCount = this.results.filter(r => r.type === 'error').length;
        const warningCount = this.results.filter(r => r.type === 'warning').length;
        
        const report = {
            summary: {
                total: this.results.length,
                success: successCount,
                errors: errorCount,
                warnings: warningCount,
                score: Math.round((successCount / (successCount + errorCount)) * 100) || 0
            },
            details: this.results
        };
        
        console.log('=== ONLINE USERS SYSTEM TEST REPORT ===');
        console.log(`Score: ${report.summary.score}%`);
        console.log(`Success: ${successCount}, Errors: ${errorCount}, Warnings: ${warningCount}`);
        console.log('Full report:', report);
        
        return report;
    }
}

// Auto-run tests when script is loaded
if (typeof window !== 'undefined') {
    window.OnlineUsersTest = OnlineUsersTest;
    
    // Add a global function to run tests
    window.runOnlineUsersTest = async function() {
        const tester = new OnlineUsersTest();
        await tester.runAllTests();
        return tester.generateReport();
    };
    
    console.log('Online Users Test loaded. Run window.runOnlineUsersTest() to start testing.');
}
